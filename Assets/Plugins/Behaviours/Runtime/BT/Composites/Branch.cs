using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Branch")]
	public class Branch : Composite {
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
