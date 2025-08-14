using System;
using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Inverter")]
	[SearchPath("Decorators/Inverter")]
	public class Inverter : Decorator {
		protected override ExecutionStatus OnEnter() {
			return m_child.EnterSequence() switch {
				ExecutionStatus.Running => ExecutionStatus.Running,
				ExecutionStatus.Success => ExecutionStatus.Failure,
				ExecutionStatus.Failure => ExecutionStatus.Success,
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		protected override ExecutionStatus OnChildFinished() {
			return m_child.Status switch {
				BehaviourStatus.Success => ExecutionStatus.Failure,
				BehaviourStatus.Failure => ExecutionStatus.Success,
				_ => throw new ArgumentOutOfRangeException(),
			};
		}
	}
}
