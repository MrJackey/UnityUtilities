using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Conditional")]
	public class Conditional : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

		public override string Editor_Info => m_conditions?.Editor_Info;

		protected override ExecutionStatus OnEnter() {
			if (!m_conditions.Evaluate())
				return ExecutionStatus.Failure;

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_child.IsFinished)
				return (ExecutionStatus)m_child.Status;

			Debug.Assert(m_child.Status == ActionStatus.Inactive);
			ExecutionStatus enterStatus = m_child.Enter();

			if (m_child.IsFinished)
				return enterStatus;

			m_child.Tick();
			return (ExecutionStatus)m_child.Status;
		}
	}
}
