using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Evaluate")]
	public class Evaluate : BehaviourAction {
		[SerializeField] private BehaviourConditionGroup m_conditions;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("Evaluate")}\n" +
		                                      $"{m_conditions?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			bool result = m_conditions.Evaluate();
			m_conditions.Disable();

			return result ? ExecutionStatus.Success : ExecutionStatus.Failure;
		}
	}
}
