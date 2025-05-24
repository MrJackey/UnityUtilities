using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[DisplayName("Do Until")]
	[SearchPath("Composites/Do Until")]
	public class DoUntil : Composite {
		private int m_runningIndex;

		protected override ExecutionStatus OnEnter() {
			// Check overriding children
			for (int i = 1; i < m_children.Count; i++) {
				ExecutionStatus overrideStatus = m_children[i].EnterSequence();
				if (overrideStatus == ExecutionStatus.Success)
					return ExecutionStatus.Success;

				if (overrideStatus == ExecutionStatus.Running) {
					m_runningIndex = i;
					return ExecutionStatus.Running;
				}
			}

			// Start the base child
			ExecutionStatus childStatus = m_children[0].EnterSequence();
			if (childStatus == ExecutionStatus.Running) {
				m_runningIndex = 0;
				EnableTicking();
			}

			return childStatus;
		}

		protected override ExecutionStatus OnTick() {
			for (int i = 1; i < m_children.Count; i++) {
				ExecutionStatus overrideStatus = m_children[i].EnterSequence();
				if (overrideStatus == ExecutionStatus.Success)
					return ExecutionStatus.Success;

				if (overrideStatus == ExecutionStatus.Running) {
					m_children[0].Interrupt();

					DisableTicking();
					m_runningIndex = i;
					return ExecutionStatus.Running;
				}
			}

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnChildFinished() {
			return (ExecutionStatus)m_children[m_runningIndex].Status;
		}

		protected override void OnExit() {
			if (m_runningIndex == 0)
				DisableTicking();
		}
	}
}
