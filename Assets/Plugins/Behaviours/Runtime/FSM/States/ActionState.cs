using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Actions;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[SearchPath("Action State")]
	public class ActionState : BehaviourState {
		[SerializeReference] private BehaviourAction m_action;

#if UNITY_EDITOR
		public override string Editor_Info => m_action?.Editor_Info;
#endif

		protected internal override bool ShouldTick => m_action.IsTicking;

		internal override void Initialize(StateMachine behaviour) {
			base.Initialize(behaviour);

			m_action.FSM_Initialize(behaviour);
		}

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
