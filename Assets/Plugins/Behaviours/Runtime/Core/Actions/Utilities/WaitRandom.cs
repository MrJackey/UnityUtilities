using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Wait Random")]
	public class WaitRandom : BehaviourAction {
		[SerializeField] private BlackboardRef<float> m_min;
		[SerializeField] private BlackboardRef<float> m_max;
		[SerializeField] private bool m_unscaledTime;

		private float m_waitTime;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait {m_min.Editor_Info}-{m_max.Editor_Info}s";
#endif

		protected override ExecutionStatus OnEnter() {
			float enterTime = m_unscaledTime ? Time.unscaledTime : Time.time;
			m_waitTime = enterTime + Random.Range(m_min.GetValue(), m_max.GetValue());

			EnableTicking();
			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			float time = m_unscaledTime ? Time.unscaledTime : Time.time;

			if (time >= m_waitTime)
				return ExecutionStatus.Success;

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			DisableTicking();
		}
	}
}
