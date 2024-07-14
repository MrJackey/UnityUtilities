using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Branch")]
	[SearchPath("Composites/Branch")]
	public class Branch : Composite {
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
