using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Wait Until")]
	public class WaitUntil : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

		private bool m_isTicking;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("Wait Until")}\n" +
		                                      $"{m_conditions?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);

			if (m_conditions.Evaluate()) {
				m_conditions.Disable();
				return m_child?.EnterSequence() ?? ExecutionStatus.Success;
			}

			EnableTicking();
			m_isTicking = true;

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_conditions.Evaluate()) {
				m_conditions.Disable();
				DisableTicking();
				m_isTicking = false;

				return m_child?.EnterSequence() ?? ExecutionStatus.Success;
			}

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			if (m_isTicking) {
				m_conditions.Disable();
				DisableTicking();
			}
		}
	}
}
