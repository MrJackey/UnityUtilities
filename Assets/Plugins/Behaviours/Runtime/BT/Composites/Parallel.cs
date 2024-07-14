using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Parallel")]
	[SearchPath("Composites/Parallel")]
	public class Parallel : Composite {
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
