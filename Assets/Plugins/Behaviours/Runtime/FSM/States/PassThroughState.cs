using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.FSM.States {
	[SearchPath("Utilities/Pass Through State")]
	public class PassThroughState : BehaviourState {
		protected override ExecutionStatus OnEnter() {
			return ExecutionStatus.Success;
		}
	}
}
