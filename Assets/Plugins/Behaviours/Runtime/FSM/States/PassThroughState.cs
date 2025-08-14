namespace Jackey.Behaviours.FSM.States {
	public class PassThroughState : BehaviourState {
		protected override ExecutionStatus OnEnter() {
			return ExecutionStatus.Success;
		}
	}
}
