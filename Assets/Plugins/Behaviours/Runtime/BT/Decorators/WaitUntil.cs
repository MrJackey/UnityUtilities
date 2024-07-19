using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Wait Until")]
	public class WaitUntil : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait until\n{m_conditions?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			EnableTicking();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_child.IsFinished)
				return (ExecutionStatus)m_child.Status;

			if (m_conditions.Evaluate()) {
				DisableTicking();
				return m_child.EnterSequence();
			}

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			m_conditions.Disable();
		}
	}
}
