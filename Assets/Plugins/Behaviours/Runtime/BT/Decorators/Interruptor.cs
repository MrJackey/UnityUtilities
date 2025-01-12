using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Interruptor")]
	public class Interruptor : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

		[Space]
		[SerializeField] private ActionResult m_interruptResult = ActionResult.Failure;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("Interrupt if")}\n" +
		                                      $"{m_conditions?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			if (m_child == null)
				return ExecutionStatus.Success;

			m_conditions.Enable(Owner);
			EnableTicking();
			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_child.IsFinished)
				return (ExecutionStatus)m_child.Status;

			if (m_conditions.Evaluate()) {
				m_child.Interrupt();
				return (ExecutionStatus)m_interruptResult;
			}

			if (m_child.Status == ActionStatus.Inactive)
				return m_child.EnterSequence();

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			m_conditions.Disable();
			DisableTicking();
		}
	}
}
