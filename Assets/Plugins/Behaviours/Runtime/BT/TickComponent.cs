using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT {
	[SearchPath("Tick Component")]
	public class TickComponent : BehaviourAction {
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
