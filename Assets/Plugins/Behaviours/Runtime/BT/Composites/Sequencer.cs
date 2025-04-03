using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Sequencer")]
	[SearchPath("Composites/Sequencer")]
	public class Sequencer : Composite {
		private int m_runningIndex;

		protected override ExecutionStatus OnEnter() {
			m_runningIndex = -1;
			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			// Check the running child
			if (m_runningIndex != -1) {
				ActionStatus runningStatus = m_children[m_runningIndex].Status;

				if (runningStatus == ActionStatus.Failure)
					return ExecutionStatus.Failure;

				// Running || Success
			}

			// Continue the sequence
			while (m_runningIndex < m_children.Count - 1) {
				m_runningIndex++;

				ExecutionStatus enterStatus = m_children[m_runningIndex].EnterSequence();

				if (enterStatus == ExecutionStatus.Failure)
					return ExecutionStatus.Failure;

				if (enterStatus == ExecutionStatus.Success)
					continue;

				return ExecutionStatus.Running;
			}

			return ExecutionStatus.Success;
		}
	}
}
