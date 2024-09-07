using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Selector")]
	[SearchPath("Composites/Selector")]
	public class Selector : Composite {
		private int m_runningIndex;

		protected override ExecutionStatus OnEnter() {
			m_runningIndex = -1;
			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			// Check the running child
			if (m_runningIndex != -1) {
				ActionStatus runningStatus = Children[m_runningIndex].Status;

				if (runningStatus != ActionStatus.Failure)
					return (ExecutionStatus)runningStatus;

				// Failure
			}

			// Check next child in sequence
			while (m_runningIndex < Children.Count - 1) {
				m_runningIndex++;

				ExecutionStatus enterStatus = m_children[m_runningIndex].EnterSequence();

				if (enterStatus == ExecutionStatus.Failure)
					continue;

				if (enterStatus == ExecutionStatus.Success)
					return ExecutionStatus.Success;

				return ExecutionStatus.Running;
			}

			return ExecutionStatus.Failure;
		}
	}
}
