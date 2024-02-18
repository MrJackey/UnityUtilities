using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Parallel")]
	public class Parallel : Composite {
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
