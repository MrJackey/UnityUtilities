using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Interruptor")]
	public class Interruptor : Decorator {
		[SerializeField] private BehaviourConditionList m_conditions;

		[Space]
		[SerializeField] private BehaviourResult m_interruptResult = BehaviourResult.Failure;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("Interrupt if")}\n" +
		                                      $"{m_conditions?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			if (m_child == null)
				return ExecutionStatus.Success;

			m_conditions.Enable(Owner);
			EnableTicking();
			return m_child.EnterSequence();
		}

		protected override ExecutionStatus OnTick() {
			if (m_conditions.Evaluate()) {
				m_child.Interrupt();
				return (ExecutionStatus)m_interruptResult;
			}

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			m_conditions.Disable();
			DisableTicking();
		}
	}
}
