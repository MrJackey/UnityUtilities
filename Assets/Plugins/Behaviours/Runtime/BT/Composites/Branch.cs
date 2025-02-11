using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Branch")]
	[SearchPath("Composites/Branch")]
	public class Branch : Composite {
		[SerializeField] private BehaviourConditionGroup m_conditions;

		private int m_runningIndex = -1;

#if UNITY_EDITOR
		public override string Editor_Info => m_conditions?.Editor_Info;
		protected internal override int Editor_MaxChildCount => 2;
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			m_runningIndex = m_conditions.Evaluate() ? 0 : 1;
			m_conditions.Disable();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			BehaviourAction runningChild = m_children[m_runningIndex];
			if (runningChild.IsFinished)
				return (ExecutionStatus)runningChild.Status;

			Debug.Assert(runningChild.Status == ActionStatus.Inactive);
			return runningChild.EnterSequence();
		}
	}
}
