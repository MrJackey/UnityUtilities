using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Events;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Wait Event")]
	public class WaitEvent : Decorator, IBehaviourEventListener {
		[SerializeField] private BehaviourEvent m_event;

		private bool m_yield;

#if UNITY_EDITOR
		public override string Editor_Info => $"Wait until: {(m_event != null ? m_event.name : "Missing Event")}";
#endif

		protected override ExecutionStatus OnEnter() {
			m_yield = false;
			Owner.AddEventListener(this);

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			Debug.Assert(m_yield);

			DisableTicking();
			m_yield = false;

			return m_child?.EnterSequence() ?? ExecutionStatus.Success;
		}

		protected override void OnExit() {
			Owner.RemoveEventListener(this);

			if (m_yield)
				DisableTicking();
		}

		void IBehaviourEventListener.OnEvent(BehaviourEvent evt) {
			if (m_yield || evt != m_event) return;

			m_yield = true;
			EnableTicking();
		}
	}
}
