using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Operations;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[SearchPath("Operation State")]
	public class OperationState : BehaviourState {
		[Header("Operations")]
		[SerializeField] private OperationList m_operations;

		protected override ExecutionStatus OnEnter() {
			m_operations.Execute(Owner);
			return ExecutionStatus.Success;
		}
	}
}
