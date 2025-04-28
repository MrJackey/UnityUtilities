using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Selector")]
	[SearchPath("Composites/Selector")]
	public class Selector : Composite {
		private int m_runningIndex;

		protected override ExecutionStatus OnEnter() {
			m_runningIndex = -1;
			return ContinueSequence();
		}

		protected override ExecutionStatus OnChildFinished() {
			ActionStatus runningStatus = Children[m_runningIndex].Status;

			if (runningStatus != ActionStatus.Failure)
				return (ExecutionStatus)runningStatus;

			return ContinueSequence();
		}

		private ExecutionStatus ContinueSequence() {
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
