using System;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Repeater")]
	[SearchPath("Decorators/Repeater")]
	public class Repeater : Decorator {
		[SerializeField] private Policy m_policy;

		[Min(0f), ShowIf(nameof(m_policy), IfAttribute.Comparison.Equal, (int)Policy.Times, order = -1)]
		[SerializeField] private int m_iterations = 1;

		[ShowIf(nameof(m_policy), IfAttribute.Comparison.Equal, (int)Policy.UntilStatus)]
		[SerializeField] private BehaviourResult m_result = BehaviourResult.Success;

		[CustomShowIf(nameof(m_policy), IfAttribute.Comparison.Equal, (int)Policy.WhileConditions)]
		[SerializeField] private BehaviourConditionGroup m_conditions;

		private bool m_isTicking;

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

			if (m_policy == Policy.WhileConditions)
				m_conditions.Enable(Owner);

			return Repeat();
		}

		protected override ExecutionStatus OnTick() {
			DisableTicking();
			m_isTicking = false;

			return Repeat();
		}

		protected override ExecutionStatus OnChildFinished() {
			return Repeat();
		}

		private ExecutionStatus Repeat() {
			ExecutionStatus repeatStatus;

			switch (m_policy) {
				case Policy.Times:
					m_completedIterations++;

					if (m_completedIterations >= m_iterations)
						return ExecutionStatus.Success;

					repeatStatus = RepeatChild();

					if (repeatStatus != ExecutionStatus.Running && m_completedIterations + 1 >= m_iterations) {
#if UNITY_EDITOR
						m_completedIterations++; // Not needed to function since the action is completed but ensures the Editor_Info is up-to-date
#endif
						return ExecutionStatus.Success;
					}

					break;
				case Policy.UntilStatus:
					if ((BehaviourResult)m_child.Status == m_result)
						return ExecutionStatus.Success;

					repeatStatus = RepeatChild();

					if ((BehaviourResult)repeatStatus == m_result)
						return ExecutionStatus.Success;

					break;
				case Policy.WhileConditions:
					if (!m_conditions.Evaluate())
						return ExecutionStatus.Success;

					repeatStatus = RepeatChild();

					if (repeatStatus != ExecutionStatus.Running && !m_conditions.Evaluate())
						return ExecutionStatus.Success;

					break;
				case Policy.Forever:
					repeatStatus = RepeatChild();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (repeatStatus != ExecutionStatus.Running) {
				EnableTicking();
				m_isTicking = true;
			}

			return ExecutionStatus.Running;
		}

		private ExecutionStatus RepeatChild() {
			if (m_child.IsFinished)
				m_child.Reset();

			return m_child.EnterSequence();
		}

		protected override void OnInterrupt() {
			if (m_isTicking) {
				DisableTicking();
				m_isTicking = false;
			}
		}

		protected override void OnExit() {
			if (m_policy == Policy.WhileConditions)
				m_conditions.Disable();
		}

		private enum Policy {
			Times,
			UntilStatus,
			WhileConditions,
			Forever,
		}
	}
}
