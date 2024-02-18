using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT {
	public class TickComponent : BehaviourAction {
		protected override ExecutionStatus OnTick() {
			return ExecutionStatus.Success;
		}
	}
}
