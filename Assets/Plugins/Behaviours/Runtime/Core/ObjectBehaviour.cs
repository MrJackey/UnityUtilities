using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Jackey.Behaviours.Actions;
using Jackey.Behaviours.Variables;
using UnityEngine;

#if UNITY_EDITOR
using Jackey.Behaviours.Attributes;
using UnityEditor;
#endif

namespace Jackey.Behaviours {
	public abstract class ObjectBehaviour : ScriptableObject {
		[SerializeField] internal Blackboard m_blackboard;

#if UNITY_EDITOR
		[SerializeField] internal EditorData Editor_Data;
#endif

		protected internal BehaviourOwner Owner { get; internal set; }
		public Blackboard Blackboard => m_blackboard;

		public BehaviourStatus Status { get; protected set; } = BehaviourStatus.Inactive;

		internal virtual void Initialize(BehaviourOwner owner) {
			Owner = owner;
		}

		internal abstract void Start();
		internal abstract ExecutionStatus Tick();
		internal abstract void Stop();

		internal virtual void EnableTicking(BehaviourAction action) { }
		internal virtual void DisableTicking(BehaviourAction action) { }

#if UNITY_EDITOR
		private void OnEnable() {
			if (!EditorUtility.IsPersistent(this)) return;

			// The current managed reference repair implementation only works on Unity Objects written to disk (see SerializationUtilities.RepairMissingManagedTypes).
			// Therefore force save all dirty behaviours to disk just before types may become invalid
			AssemblyReloadEvents.beforeAssemblyReload -= SaveToDiskBeforeReload;
			AssemblyReloadEvents.beforeAssemblyReload += SaveToDiskBeforeReload;
		}

		private void SaveToDiskBeforeReload() {
			// Prevent saving invalid assets e.g. deleted
			if (this == null) {
				AssemblyReloadEvents.beforeAssemblyReload -= SaveToDiskBeforeReload;
				return;
			}

			AssetDatabase.SaveAssetIfDirty(this);
		}

		protected virtual void OnValidate() {
			if (SerializationUtility.HasManagedReferencesWithMissingTypes(this))
				return;

			for (int i = m_blackboard.m_variables.Count - 1; i >= 0; i--) {
				if (m_blackboard.m_variables[i] == null)
					m_blackboard.m_variables.RemoveAt(i);
			}
		}

		protected void ConnectBlackboardRefs(object instance) {
			if (instance == null) return;

			Type type = instance.GetType();

			while (true) {
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (field.GetCustomAttribute<SkipBlackboardConnectAttribute>() != null) continue;

					Type fieldType = field.FieldType;

					if (!fieldType.IsSerializable) continue;
					if (fieldType.IsPrimitive) continue;

					if (fieldType.IsGenericType) {
						Type typeDefinition = fieldType.GetGenericTypeDefinition();

						if (typeDefinition == typeof(BlackboardRef<>) || typeDefinition == typeof(BlackboardOnlyRef<>)) {
							FieldInfo behaviourField = fieldType.GetField("m_behaviour", BindingFlags.Instance | BindingFlags.NonPublic);
							Debug.Assert(behaviourField != null);

							object blackboardRefValue = field.GetValue(instance);
							behaviourField.SetValue(blackboardRefValue, this);
							field.SetValue(instance, blackboardRefValue);
						}
					}

					object fieldValue = field.GetValue(instance);
					if (fieldValue == null) continue;

					// Only lists and arrays are serializable collections in Unity
					if (fieldValue is IList list) {
						foreach (object item in list)
							ConnectBlackboardRefs(item);
					}
					else {
						ConnectBlackboardRefs(fieldValue);

						if (fieldType.IsValueType)
							field.SetValue(instance, fieldValue);
					}
				}

				if (type.BaseType == null)
					break;

				type = type.BaseType;
			}
		}

		[Serializable]
		internal class EditorData {
			public List<Group> Groups = new();

			[Serializable]
			public class Group {
				[HideInInspector]
				public Rect Rect;
				[HideInInspector]
				public bool AutoSize;

				[HideInInspector]
				public string Label;
				[TextArea]
				public string Comments;
			}
		}
#endif
	}
}
