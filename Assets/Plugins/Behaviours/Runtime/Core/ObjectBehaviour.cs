using System;
using System.Collections.Generic;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

#if UNITY_EDITOR
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

		internal virtual void Initialize(BehaviourOwner owner) {
			Owner = owner;
		}

		internal abstract void Start();
		internal abstract ExecutionStatus Tick();
		internal abstract void Stop();

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
