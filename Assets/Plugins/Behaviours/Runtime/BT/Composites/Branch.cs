using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Branch")]
	[SearchPath("Composites/Branch")]
	public class Branch : Composite {
		[SerializeField] private bool m_dynamic;
		[SerializeField] private BehaviourConditionGroup m_conditions;

		private int m_runningIndex = -1;

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (!m_dynamic)
					return m_conditions?.Editor_Info;

				return $"{InfoUtilities.AlignCenter("<b>DYNAMIC</b>")}\n" +
				       $"Switch {m_conditions?.Editor_Info}";
			}
		}

		protected internal override int Editor_MaxChildCount => 2;
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			m_runningIndex = m_conditions.Evaluate() ? 1 : 0;

			if (m_dynamic)
				EnableTicking();
			else
				m_conditions.Disable();

			return m_children[m_runningIndex].EnterSequence();
		}

		protected override ExecutionStatus OnTick() {
			int index = m_conditions.Evaluate() ? 1 : 0;

			if (index == m_runningIndex)
				return ExecutionStatus.Running;

			m_children[m_runningIndex].Interrupt();

			m_runningIndex = index;
			BehaviourAction nextChild = m_children[m_runningIndex];

			if (nextChild.Status != ActionStatus.Inactive)
				nextChild.Reset();

			return nextChild.EnterSequence();
		}

		protected override ExecutionStatus OnChildFinished() {
			return (ExecutionStatus)m_children[m_runningIndex].Status;
		}

		protected override void OnExit() {
			if (m_dynamic) {
				m_conditions.Disable();
				DisableTicking();
			}
		}
	}
}
