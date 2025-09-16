using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Wait")]
	public class Wait : BehaviourAction {
		public BlackboardRef<float> Duration;
		public bool UnscaledTime;

		private float m_enterTime;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait {Duration.Editor_Info}s";
#endif

		protected override ExecutionStatus OnEnter() {
			m_enterTime = UnscaledTime ? Time.unscaledTime : Time.time;
			EnableTicking();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			float time = UnscaledTime ? Time.unscaledTime : Time.time;

			if (time >= m_enterTime + Duration.GetValue())
				return ExecutionStatus.Success;

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			DisableTicking();
		}
	}
}
