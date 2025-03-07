﻿using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.BT {
	[BehaviourType]
	[CreateAssetMenu(fileName = "new BehaviourTree", menuName = "Jackey/Behaviour/Behaviour Tree", order = 0)]
	public class BehaviourTree : ObjectBehaviour {
		[SerializeReference] internal List<BehaviourAction> m_allActions = new();
		[SerializeReference] internal BehaviourAction m_entry;

		private List<BehaviourAction> m_tickingActions = new();
		private List<BehaviourAction> m_pendingTickingActions = new();

		private bool m_inTickLoop;
		private int m_tickIndex;

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

			Status = (ActionStatus)m_entry.EnterSequence();
		}

		internal override ExecutionStatus Tick() {
			m_inTickLoop = true;
			for (m_tickIndex = 0; m_tickIndex < m_tickingActions.Count; m_tickIndex++) {
				int i = m_tickIndex;

				BehaviourAction action = m_tickingActions[i];

				ExecutionStatus actionStatus = action.TickSequence();

				if (actionStatus == ExecutionStatus.Running)
					continue;

				BehaviourAction parent = action.Parent;

				// The entire tree has finished
				if (parent == null) {
					m_inTickLoop = false;
					return actionStatus;
				}

				// Traverse the tree upwards to find the next branch
				while (true) {
					ExecutionStatus parentStatus = parent.TickSequence();

					if (parentStatus == ExecutionStatus.Running)
						break;

					parent = parent.Parent;

					// The entire tree has finished
					if (parent == null) {
						m_inTickLoop = false;
						return parentStatus;
					}
				}
			}

			m_inTickLoop = false;

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
			if (m_inTickLoop) {
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
				// Ensure that any currently ticking or just ticked actions does not skip any actions within the tick loop
				if (m_inTickLoop && tickingIndex <= m_tickIndex)
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
						if (decorator.Child != null)
							Inner(decorator.Child);

						break;
				}
			}

			Inner(m_entry);
		}
#endif
	}
}
