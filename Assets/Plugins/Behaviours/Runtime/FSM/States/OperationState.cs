using Jackey.Behaviours.Core.Operations;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	public class OperationState : BehaviourState {
		[SerializeField] private OperationList m_operations;

		protected override ExecutionStatus OnEnter() {
			m_operations.Execute(Owner);
			return ExecutionStatus.Success;
		}
	}
}
