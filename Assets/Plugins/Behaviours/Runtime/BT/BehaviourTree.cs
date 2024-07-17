using System.Collections.Generic;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.BT {
	[CreateAssetMenu(fileName = "new BehaviourTree", menuName = "Jackey/Behaviour/Behaviour Tree", order = 0)]
	public class BehaviourTree : ObjectBehaviour {
		[SerializeReference] internal List<BehaviourAction> m_allActions;
		[SerializeReference] internal BehaviourAction m_entry;

		private List<BehaviourAction> m_tickingActions = new();

		public ActionStatus Status { get; private set; } = ActionStatus.Inactive;

		internal override void Initialize(BehaviourOwner owner) {
			if (m_entry == null) {
				Debug.LogError("Behaviour Tree does not have an entry action. Unable to initialize", this);
				return;
			}

			base.Initialize(owner);

			int index = 0;
			m_entry.Initialize(this, null, ref index);
			Debug.Log($"Initialized BehaviourTree with {index + 1} nodes");
		}

		internal override void Start() {
			if (Status != ActionStatus.Inactive)
				return;

			Status = ActionStatus.Running;

			m_entry.EnterSequence();
		}

		internal override ExecutionStatus Tick() {
			for (int i = 0; i < m_tickingActions.Count; i++) {
				BehaviourAction action = m_tickingActions[i];

				ExecutionStatus actionStatus = action.TickSequence();

				if (actionStatus == ExecutionStatus.Running)
					continue;

				BehaviourAction parent = action.Parent;

				// The entire tree has finished
				if (parent == null)
					return actionStatus;

				while (true) {
					ExecutionStatus parentStatus = parent.TickSequence();

					if (parentStatus == ExecutionStatus.Running)
						break;

					parent = parent.Parent;

					if (parent == null)
						return parentStatus;
				}
			}

			return ExecutionStatus.Running;
		}

		internal override void Stop() {
			m_entry.Reset();
			Status = ActionStatus.Inactive;
		}

		public void EnableTicking(BehaviourAction action) {
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
			m_tickingActions.Remove(action);
		}

		private void Reset() {
			m_entry = null;
			m_allActions?.Clear();
		}

#if UNITY_EDITOR
		private void OnValidate() {
			if (m_entry != null) {
				ValidateDetachedActions();
			}

			for (int i = m_blackboard.m_variables.Count - 1; i >= 0; i--) {
				if (m_blackboard.m_variables[i] == null)
					m_blackboard.m_variables.RemoveAt(i);
			}
		}

		private void ValidateDetachedActions() {
			if (m_entry == null) return;

			void Inner(BehaviourAction current) {
				if (!m_allActions.Contains(current))
					Debug.LogError("Behaviour Tree detected detached actions!");

				switch (current) {
					case Composite composite:
						foreach (BehaviourAction behaviourAction in composite.Children)
							Inner(behaviourAction);

						break;
					case Decorator decorator:
							Inner(decorator.Child);
					break;
				}
			}

			Inner(m_entry);
		}

		[ContextMenu(nameof(ClearManagedMissingTypes))]
		public void ClearManagedMissingTypes() {
			bool missingTypes = UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(this);
			bool result = UnityEditor.SerializationUtility.ClearAllManagedReferencesWithMissingTypes(this);
			Debug.Log(new { missingTypes, result });
		}
#endif
	}
}
