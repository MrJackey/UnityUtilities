using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[GraphIcon("Cooldown")]
	[SearchPath("Decorators/Cooldown")]
	public class Cooldown : Decorator {
		public BlackboardRef<float> Duration;
		public bool UnscaledTime;

		private float m_nextTime;

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (m_runtimeBehaviour == null) // EditMode
					return $"Cooldown {Duration.Editor_Info}s";

				float time = UnscaledTime ? Time.unscaledTime : Time.time;
				return $"{InfoUtilities.AlignCenter($"({Mathf.Max(m_nextTime - time, 0f):00.00})")}\n" +
				       $"Cooldown {Duration.Editor_Info}s";
			}
		}
#endif

		protected override ExecutionStatus OnEnter() {
			float time = UnscaledTime ? Time.unscaledTime : Time.time;
			if (time < m_nextTime)
				return ExecutionStatus.Failure;

			ExecutionStatus childStatus = m_child.EnterSequence();

			// Reset cooldown on instant finish
			if (m_child.IsFinished)
				m_nextTime = time + Duration.GetValue();

			return childStatus;
		}

		protected override ExecutionStatus OnChildFinished() {
			float time = UnscaledTime ? Time.unscaledTime : Time.time;
			m_nextTime = time + Duration.GetValue();

			return (ExecutionStatus)m_child.Status;
		}
	}
}
