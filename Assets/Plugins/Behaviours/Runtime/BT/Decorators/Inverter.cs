using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Inverter")]
	[SearchPath("Decorators/Inverter")]
	public class Inverter : Decorator {
		protected override ExecutionStatus OnTick() {
			if (!m_child.IsFinished)
				m_child.EnterSequence();

			return m_child.Status switch {
				ActionStatus.Success => ExecutionStatus.Failure,
				ActionStatus.Failure => ExecutionStatus.Success,
				_ => ExecutionStatus.Running,
			};
		}
	}
}
