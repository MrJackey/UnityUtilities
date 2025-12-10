using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[GraphIcon("Conditional")]
	[SearchPath("Conditional State")]
	public class ConditionalState : BehaviourState {
		[Header("Conditions")]
		[SerializeField] private BehaviourConditionList m_conditions;

#if UNITY_EDITOR
		public override string Editor_Info => m_conditions.Editor_Info;
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			bool condition = m_conditions.Evaluate();
			m_conditions.Disable();

			return condition ? ExecutionStatus.Success : ExecutionStatus.Failure;
		}
	}
}
