using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Composites {
	[SearchPath("Composites/Switch")]
	public class Switch : Composite {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<int> m_index;
		[SerializeField] private bool m_dynamic;

		private int m_activeIndex;

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (!m_dynamic)
					return $"Switch {m_index.Editor_Info}";

				return $"{InfoUtilities.AlignCenter("<b>DYNAMIC</b>")}\n" +
				       $"Switch {m_index.Editor_Info}";
			}
		}
#endif

		protected override ExecutionStatus OnEnter() {
			if (m_children.Count == 0) {
				Debug.LogWarning("Behaviour switch does not have any children", Owner);
				return ExecutionStatus.Failure;
			}

			m_activeIndex = -1;

			if (m_dynamic)
				EnableTicking();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_activeIndex != -1) {
				BehaviourAction activeChild = m_children[m_activeIndex];

				if (activeChild.IsFinished)
					return (ExecutionStatus)activeChild.Status;
			}

			int tickValue = m_index.GetValue();

			if (tickValue < 0 || tickValue > m_children.Count - 1) {
				Debug.LogError($"Behaviour switch ticked with an invalid value: {tickValue}. Valid: [0, {m_children.Count})", Owner);
				return ExecutionStatus.Failure;
			}

			if (tickValue != m_activeIndex)
				return SwitchChild(tickValue);

			return ExecutionStatus.Running;
		}

		private ExecutionStatus SwitchChild(int to) {
			if (m_activeIndex != -1)
				m_children[m_activeIndex].Interrupt();

			m_activeIndex = to;
			BehaviourAction nextChild = m_children[m_activeIndex];

			if (nextChild.Status != ActionStatus.Inactive)
				nextChild.Reset();

			return nextChild.EnterSequence();
		}

		protected override void OnExit() {
			if (m_dynamic)
				DisableTicking();
		}
	}
}
