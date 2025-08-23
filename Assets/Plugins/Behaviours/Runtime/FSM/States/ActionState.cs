using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[SearchPath("Action State")]
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

		internal void SetAction(BehaviourAction action) {
			m_action = action;
		}
	}
}
