using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Conditional")]
	[SearchPath("Decorators/Conditional")]
	public class Conditional : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions = new();

		internal BehaviourConditionGroup Conditions => m_conditions;
		public override string Editor_Info => m_conditions?.Editor_Info;

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			bool condition = m_conditions.Evaluate();
			m_conditions.Disable();

			if (!condition)
				return ExecutionStatus.Failure;

			return m_child.EnterSequence();
		}
	}
}
