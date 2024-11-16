using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Operations;
using UnityEngine;

namespace Jackey.Behaviours.BT.Actions {
	[SearchPath("Operator")]
	public class Operator : BehaviourAction {
		[SerializeField] private OperationList m_operations;

#if UNITY_EDITOR
		public override string Editor_Info => m_operations?.Editor_Info;
#endif

		protected override ExecutionStatus OnEnter() {
			m_operations.Execute(Owner);
			return ExecutionStatus.Success;
		}
	}
}
