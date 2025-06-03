using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Cooldown")]
	[SearchPath("Decorators/Cooldown")]
	public class Cooldown : Decorator {
		[SerializeField] private BlackboardRef<float> m_duration;
		[SerializeField] private bool m_unscaledTime;

		[Space]
		[SerializeField] private Policy m_policy;

		private float m_nextTime;

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (m_runtimeBehaviour == null) // EditMode
					return $"Cooldown {m_duration.Editor_Info}s";

				float time = m_unscaledTime ? Time.unscaledTime : Time.time;
				return $"{InfoUtilities.AlignCenter($"({Mathf.Max(m_nextTime - time, 0f):00.00})")}\n" +
				       $"Cooldown {m_duration.Editor_Info}s";
			}
		}
#endif

		protected override ExecutionStatus OnEnter() {
			float time = m_unscaledTime ? Time.unscaledTime : Time.time;
			if (time < m_nextTime)
				return ExecutionStatus.Failure;

			ExecutionStatus childStatus = m_child.EnterSequence();

			if (m_policy == Policy.ResetOnFinish && m_child.IsFinished)
				m_nextTime = time + m_duration.GetValue();

			return childStatus;
		}

		protected override ExecutionStatus OnChildFinished() {
			float time = m_unscaledTime ? Time.unscaledTime : Time.time;
			m_nextTime = time + m_duration.GetValue();

			return (ExecutionStatus)m_child.Status;
		}

		private enum Policy {
			ResetOnFinish,
			ResetOnTickFinish,
		}
	}
}
