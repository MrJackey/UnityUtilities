﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours.BT {
	[BehaviourType]
	[CreateAssetMenu(fileName = "new BehaviourTree", menuName = "Jackey/Behaviour/Behaviour Tree", order = 0)]
	public class BehaviourTree : ObjectBehaviour {
		[SerializeReference] internal List<BehaviourAction> m_allActions = new();
		[SerializeReference] internal BehaviourAction m_entry;

		private List<BehaviourAction> m_tickingActions = new();
		private List<BehaviourAction> m_pendingTickingActions = new();

		private bool m_inTreeTraversal;
		private int m_tickIndex;

		public ActionStatus Status { get; private set; } = ActionStatus.Inactive;

		internal override void Initialize(BehaviourOwner owner) {
			if (m_entry == null) {
				Debug.LogError("Behaviour Tree does not have an entry action. Unable to initialize", this);
				return;
			}

			base.Initialize(owner);

			int index = 0;
			m_entry.Initialize(this, null, ref index);
		}

		internal override void Start() {
			if (Status != ActionStatus.Inactive)
				return;

			Status = ActionStatus.Running;

			m_inTreeTraversal = true;
			m_entry.EnterSequence();
			m_inTreeTraversal = false;
		}

		internal override ExecutionStatus Tick() {
			m_inTreeTraversal = true;
			for (m_tickIndex = 0; m_tickIndex < m_tickingActions.Count; m_tickIndex++) {
				int i = m_tickIndex;

				BehaviourAction action = m_tickingActions[i];

				ExecutionStatus actionStatus = action.TickSequence();

				if (actionStatus == ExecutionStatus.Running)
					continue;

				BehaviourAction parent = action.Parent;

				// The entire tree has finished
				if (parent == null) {
					m_inTreeTraversal = false;
					return actionStatus;
				}

				// Traverse the tree upwards to find the next branch
				while (true) {
					ExecutionStatus parentStatus = parent.TraversalSequence();

					if (parentStatus == ExecutionStatus.Running)
						break;

					parent = parent.Parent;

					// The entire tree has finished
					if (parent == null) {
						m_inTreeTraversal = false;
						return parentStatus;
					}
				}
			}

			m_inTreeTraversal = false;

			if (m_pendingTickingActions.Count > 0) {
				foreach (BehaviourAction action in m_pendingTickingActions)
					InsertTickingAction(action);

				m_pendingTickingActions.Clear();
			}

			return ExecutionStatus.Running;
		}

		internal override void Stop() {
			m_entry.Interrupt();
			m_entry.Reset();
			Status = ActionStatus.Inactive;
		}

		public void EnableTicking(BehaviourAction action) {
			if (m_inTreeTraversal) {
				m_pendingTickingActions.Add(action);
				return;
			}

			InsertTickingAction(action);
		}

		private void InsertTickingAction(BehaviourAction action) {
			if (m_tickingActions.Count == 0) {
				m_tickingActions.Add(action);
				return;
			}

			for (int i = 0; i < m_tickingActions.Count; i++) {
				if (m_tickingActions[i].Index > action.Index) {
					m_tickingActions.Insert(i, action);
					return;
				}
			}

			m_tickingActions.Add(action);
		}

		public void DisableTicking(BehaviourAction action) {
			m_pendingTickingActions.Remove(action);

			int tickingIndex = m_tickingActions.IndexOf(action);

			if (tickingIndex != -1) {
				// Ensure that any currently ticking or just ticked actions does not skip any actions whilst traversing the tree
				if (m_inTreeTraversal && tickingIndex <= m_tickIndex)
					m_tickIndex--;

				m_tickingActions.RemoveAt(tickingIndex);
			}
		}

		private void Reset() {
			m_entry = null;
			m_allActions?.Clear();
		}

#if UNITY_EDITOR
		private void OnValidate() {
			if (UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(this))
				return;

			ConnectBlackboardRefs();

			if (m_entry != null && !m_allActions.Contains(m_entry))
				m_entry = null;

			for (int i = m_blackboard.m_variables.Count - 1; i >= 0; i--) {
				if (m_blackboard.m_variables[i] == null)
					m_blackboard.m_variables.RemoveAt(i);
			}
		}

		internal void ConnectBlackboardRefs() {
			void Inner(Type type, object instance) {
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
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
							Inner(item.GetType(), item);
					}
					else {
						Inner(fieldType, fieldValue);

						if (fieldType.IsValueType)
							field.SetValue(instance, fieldValue);
					}
				}
			}

			foreach (BehaviourAction action in m_allActions) {
				Inner(action.GetType(), action);
			}
		}
#endif
	}
}
