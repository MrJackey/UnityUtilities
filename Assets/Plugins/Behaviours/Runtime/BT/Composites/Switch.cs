using Jackey.Behaviours.Actions;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Utilities;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.BT.Composites {
	[SearchPath("Composites/Switch")]
	public class Switch : Composite {
		[SerializeField] private BlackboardOnlyRef<int> m_index;
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

			int indexValue = m_index.GetValue();

			if (indexValue < 0 || indexValue > m_children.Count - 1) {
				Debug.LogError($"Behaviour switch entered with an invalid value: {indexValue}. Valid: [0, {m_children.Count})", Owner);
				return ExecutionStatus.Failure;
			}

			m_activeIndex = indexValue;
			ExecutionStatus childStatus = m_children[m_activeIndex].EnterSequence();

			if (m_dynamic && childStatus == ExecutionStatus.Running)
				EnableTicking();

			return childStatus;
		}

		protected override ExecutionStatus OnTick() {
			int indexValue = m_index.GetValue();

			if (indexValue < 0 || indexValue > m_children.Count - 1) {
				Debug.LogError($"Behaviour switch ticked with an invalid value: {indexValue}. Valid: [0, {m_children.Count})", Owner);
				return ExecutionStatus.Failure;
			}

			if (indexValue != m_activeIndex)
				return SwitchChild(indexValue);

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnChildFinished() {
			return (ExecutionStatus)m_children[m_activeIndex].Status;
		}

		private ExecutionStatus SwitchChild(int to) {
			m_children[m_activeIndex].Interrupt();

			m_activeIndex = to;
			BehaviourAction nextChild = m_children[m_activeIndex];

			if (nextChild.Status != BehaviourStatus.Inactive)
				nextChild.Reset();

			return nextChild.EnterSequence();
		}

		protected override void OnExit() {
			if (m_dynamic)
				DisableTicking();
		}
	}
}
