using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	public class ActionState : BehaviourState {
		[SerializeReference] private BehaviourAction m_action;

		protected override ExecutionStatus OnEnter() {
			return m_action.EnterSequence();
		}

		protected override ExecutionStatus OnTick() {
			return m_action.TickSequence();
		}

		protected override void OnInterrupt() {
			m_action.Interrupt();
		}
	}
}
