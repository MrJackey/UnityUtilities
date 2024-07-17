using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Branch")]
	[SearchPath("Composites/Branch")]
	public class Branch : Composite {
#if UNITY_EDITOR
		protected internal override int Editor_MaxChildCount => 2;
#endif

		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
