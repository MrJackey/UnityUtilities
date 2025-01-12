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
				if (m_behaviour == null) // EditMode
					return $"Cooldown {Duration.Editor_Info}s";

				float time = UnscaledTime ? Time.unscaledTime : Time.time;
				return $"{InfoUtilities.AlignCenter($"({Mathf.Max(m_nextTime - time, 0f):00.00})")}\n" +
				       $"Cooldown {Duration.Editor_Info}s";
			}
		}
#endif

		protected override ExecutionStatus OnTick() {
			float time = UnscaledTime ? Time.unscaledTime : Time.time;

			// Reset cooldown on delayed child finish
			if (m_child.IsFinished) {
				m_nextTime = time + Duration.GetValue();
				return (ExecutionStatus)m_child.Status;
			}

			if (time < m_nextTime)
				return ExecutionStatus.Failure;

			Debug.Assert(m_child.Status == ActionStatus.Inactive);
			ExecutionStatus enterStatus = m_child.EnterSequence();

			// Reset cooldown on instant child finish
			if (m_child.IsFinished)
				m_nextTime = time + Duration.GetValue();

			return enterStatus;
		}
	}
}
