using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Operations;
using UnityEngine;

namespace Jackey.Behaviours.Actions {
	[SearchPath("Utilities/Operator")]
	public class Operator : BehaviourAction {
		[SerializeField] private BehaviourOperationList m_operations = new();

		internal BehaviourOperationList Operations => m_operations;

#if UNITY_EDITOR
		public override string Editor_Info => m_operations?.Editor_Info;
#endif

		protected override ExecutionStatus OnEnter() {
			m_operations.Execute(Owner);
			return ExecutionStatus.Success;
		}
	}
}
