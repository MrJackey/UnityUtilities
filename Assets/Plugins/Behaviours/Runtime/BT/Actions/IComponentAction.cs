namespace Jackey.Behaviours.BT.Actions {
	public interface IComponentAction {
		ExecutionStatus OnEnter();
		ExecutionStatus OnTick();

		void OnInterrupt();
		void OnResult(ActionResult result);
		void OnExit();
	}

	public interface IComponentAction<T> {
		ExecutionStatus OnEnter(T args);
		ExecutionStatus OnTick(T args);

		void OnInterrupt(T args);
		void OnResult(T args, ActionResult result);
		void OnExit(T args);
	}
}
