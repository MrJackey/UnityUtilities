using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Sequencer")]
	[SearchPath("Composites/Sequencer")]
	public class Sequencer : Composite {
		private int m_runningIndex;

		protected override ExecutionStatus OnEnter() {
			m_runningIndex = -1;
			return ContinueSequence();
		}

		protected override ExecutionStatus OnChildFinished() {
			ActionStatus childStatus = m_children[m_runningIndex].Status;

			if (childStatus == ActionStatus.Failure)
				return ExecutionStatus.Failure;

			return ContinueSequence();
		}

		private ExecutionStatus ContinueSequence() {
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
