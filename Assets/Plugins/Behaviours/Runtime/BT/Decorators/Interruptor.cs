using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Interruptor")]
	public class Interruptor : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

		[Space]
		[SerializeField] private ActionResult m_interruptResult = ActionResult.Failure;

		public override string Editor_Info => $"<align=\"center\">Interrupt if</align>\n{m_conditions?.Editor_Info}";

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
