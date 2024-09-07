using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Events;
using UnityEngine;

namespace Jackey.Behaviours.BT.Actions.Events {
	[SearchPath("Events/Send Event")]
	public class SendEvent : BehaviourAction<BehaviourOwner> {
		[SerializeField] private BehaviourEvent m_event;

		public override string Editor_Info {
			get {
				if (m_event == null)
					return "Send Event: Missing Event";

				return $"Send {m_event.name} to {TargetInfo}";
			}
		}

		protected override ExecutionStatus OnEnter() {
			GetTarget().SendEvent(m_event);

			return ExecutionStatus.Success;
		}
	}
}
