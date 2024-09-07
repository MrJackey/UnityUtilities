using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Events;
using UnityEngine;

namespace Jackey.Behaviours.Core.Conditions {
	[SearchPath("Utilities/Check Event")]
	public class CheckEvent : BehaviourCondition, IBehaviourEventListener {
		[SerializeField] private BehaviourEvent m_event;

		private BehaviourOwner m_owner;
		private bool m_yield;

#if UNITY_EDITOR
		public override string Editor_Info => m_event != null ? $"Event: {m_event.name}" : "Event: Missing Event";
#endif

		public override void OnEnable(BehaviourOwner owner) {
			m_owner = owner;
			m_owner.AddEventListener(this);
		}

		public override void OnDisable() {
			m_owner.RemoveEventListener(this);
			m_yield = false;
		}

		public override bool Evaluate() {
			return m_yield;
		}

		void IBehaviourEventListener.OnEvent(BehaviourEvent evt) {
			if (evt == m_event)
				m_yield = true;
		}
	}
}
