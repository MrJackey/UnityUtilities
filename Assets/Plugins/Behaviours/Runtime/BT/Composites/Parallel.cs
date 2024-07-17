using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Parallel")]
	[SearchPath("Composites/Parallel")]
	public class Parallel : Composite {
#if UNITY_EDITOR
		protected internal override int Editor_MaxChildCount => 32;
#endif
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
