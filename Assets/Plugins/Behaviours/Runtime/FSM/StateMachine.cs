using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT;
using UnityEngine;

namespace Jackey.Behaviours.FSM {
	[BehaviourType]
	[CreateAssetMenu(fileName = "new StateMachine", menuName = "Jackey/Behaviour/State Machine", order = 0)]
	public class StateMachine : ObjectBehaviour {
		[SerializeReference] internal List<BehaviourState> m_allStates = new();
		[SerializeReference] internal BehaviourState m_entry;

		private BehaviourState m_activeState;

		internal override void Initialize(BehaviourOwner owner) {
			if (m_entry == null) {
				Debug.LogError("State machine does not have an entry state. Unable to initialize", this);
				return;
			}

			base.Initialize(owner);
		}

		internal override void Start() {
			if (Status != ActionStatus.Inactive)
				return;

			Status = (ActionStatus)m_entry.EnterSequence();
		}

		internal override ExecutionStatus Tick() {
			ExecutionStatus tickStatus = m_activeState.TickSequence();

			if (tickStatus == ExecutionStatus.Running)
				return ExecutionStatus.Running;

			// Success || Failure

			// If there is no state to transition to, consider the fsm finished
			if (!m_activeState.TryGetNextState(out BehaviourState nextState))
				return ExecutionStatus.Success;

			return nextState.EnterSequence();
		}

		internal override void Stop() {
			m_activeState.Interrupt();
			Status = ActionStatus.Inactive;
		}
	}
}
