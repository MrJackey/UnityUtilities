using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours.BT.Actions.Utility {
	[SearchPath("Utilities/Wait")]
	public class Wait : BehaviourAction {
		public BlackboardRef<float> Duration;

		private float m_enterTime;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait {Duration.Editor_Info}s";
#endif

		protected override ExecutionStatus OnEnter() {
			m_enterTime = Time.time;
			EnableTicking();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (Time.time >= m_enterTime + Duration.GetValue())
				return ExecutionStatus.Success;

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			DisableTicking();
		}
	}
}
