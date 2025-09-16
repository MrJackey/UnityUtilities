using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Wait")]
	public class Wait : BehaviourAction {
		[SerializeField] private BlackboardRef<float> m_duration;
		[SerializeField] private bool m_unscaledTime;

		private float m_enterTime;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait {m_duration.Editor_Info}s";
#endif

		protected override ExecutionStatus OnEnter() {
			m_enterTime = m_unscaledTime ? Time.unscaledTime : Time.time;
			EnableTicking();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			float time = m_unscaledTime ? Time.unscaledTime : Time.time;

			if (time >= m_enterTime + m_duration.GetValue())
				return ExecutionStatus.Success;

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			DisableTicking();
		}
	}
}
