using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Operations;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[SearchPath("Operation State")]
	public class OperationState : BehaviourState {
		[Header("Operations")]
		[SerializeField] private OperationList m_operations = new();

		internal OperationList Operations => m_operations;

#if UNITY_EDITOR
		public override string Editor_Info => m_operations.Editor_Info;
#endif

		protected override ExecutionStatus OnEnter() {
			m_operations.Execute(Owner);
			return ExecutionStatus.Success;
		}
	}
}
