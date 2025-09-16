using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Resetter")]
	public class Resetter : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("Reset if")}\n" +
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
				m_child.Reset();

				return m_child.EnterSequence();
			}

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			m_conditions.Disable();
			DisableTicking();
		}
	}
}
