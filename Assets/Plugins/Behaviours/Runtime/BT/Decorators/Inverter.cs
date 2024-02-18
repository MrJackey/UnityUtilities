using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Inverter")]
	public class Inverter : Decorator {
		protected override ExecutionStatus OnTick() {
			if (!m_child.IsFinished)
				m_child.Tick();

			return m_child.Status switch {
				ActionStatus.Success => ExecutionStatus.Failure,
				ActionStatus.Failure => ExecutionStatus.Success,
				_ => ExecutionStatus.Running,
			};
		}
	}
}
