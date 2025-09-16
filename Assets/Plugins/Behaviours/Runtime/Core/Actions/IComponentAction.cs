namespace Jackey.Behaviours.Actions {
	public interface IComponentAction {
		ExecutionStatus OnEnter(BehaviourAction action);
		ExecutionStatus OnTick(BehaviourAction action);

		void OnInterrupt(BehaviourAction action);
		void OnResult(BehaviourAction action, BehaviourResult result);
		void OnExit(BehaviourAction action);
	}

	public interface IComponentAction<T> {
		ExecutionStatus OnEnter(BehaviourAction action, T args);
		ExecutionStatus OnTick(BehaviourAction action, T args);

		void OnInterrupt(BehaviourAction action, T args);
		void OnResult(BehaviourAction action, T args, BehaviourResult result);
		void OnExit(BehaviourAction action, T args);
	}
}
