using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Wait Random")]
	public class WaitRandom : BehaviourAction {
		public BlackboardRef<float> Min;
		public BlackboardRef<float> Max;
		public bool UnscaledTime;

		private float m_waitTime;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait {Min.Editor_Info}-{Max.Editor_Info}s";
#endif

		protected override ExecutionStatus OnEnter() {
			float enterTime = UnscaledTime ? Time.unscaledTime : Time.time;
			m_waitTime = enterTime + Random.Range(Min.GetValue(), Max.GetValue());

			EnableTicking();
			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			float time = UnscaledTime ? Time.unscaledTime : Time.time;

			if (time >= m_waitTime)
				return ExecutionStatus.Success;

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			DisableTicking();
		}
	}
}
