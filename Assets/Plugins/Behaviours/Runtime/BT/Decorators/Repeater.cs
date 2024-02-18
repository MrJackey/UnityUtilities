using System;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Repeater")]
	public class Repeater : Decorator {
		[SerializeField] private Policy m_policy;
		[Min(0f)]
		[SerializeField] private int m_iterations = 1;
		[SerializeField] private ActionResult m_result = ActionResult.Success;
		[SerializeField] private BehaviourConditionGroup m_conditions;

		private int m_completedIterations;

#if UNITY_EDITOR
		public override string Editor_Info => m_policy switch {
			Policy.Times => Application.isPlaying ? $"{Mathf.Max(m_completedIterations, 0)} of {m_iterations}" : $"{m_iterations} times",
			Policy.UntilStatus => $"Until {m_result}",
			Policy.WhileConditions => $"While {m_conditions.Editor_Info}",
			Policy.Forever => "Forever",
			_ => throw new ArgumentOutOfRangeException(),
		};
#endif

		protected override ExecutionStatus OnEnter() {
			m_completedIterations = -1;

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			switch (m_policy) {
				case Policy.Times:
					m_completedIterations++;

					if (m_completedIterations >= m_iterations)
						return ExecutionStatus.Success;

					RepeatOnce();
					break;
				case Policy.UntilStatus:
					if ((ActionResult)m_child.Status == m_result)
						return ExecutionStatus.Success;

					RepeatOnce();
					break;
				case Policy.WhileConditions:
					if (!m_conditions.Evaluate())
						return ExecutionStatus.Success;

					RepeatOnce();
					break;
				case Policy.Forever:
					RepeatOnce();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return ExecutionStatus.Running;
		}

		private void RepeatOnce() {
			if (m_child.IsFinished)
				m_child.Reset();

			ExecutionStatus enterStatus = m_child.Enter();

			if (enterStatus == ExecutionStatus.Running)
				m_child.Tick();
		}

		private enum Policy {
			Times,
			UntilStatus,
			WhileConditions,
			Forever,
		}
	}
}
