using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Events;
using UnityEngine;

namespace Jackey.Behaviours.Operations.Utilities {
	[SearchPath("Utilities/Send Event")]
	public class SendEvent : Operation<BehaviourOwner> {
		[SerializeField] private BehaviourEvent m_event;

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (m_event == null)
					return "Send Event: Missing Event";

				return $"Send {m_event.name} to {Editor_TargetInfo}";
			}
		}
#endif

		protected override void OnExecute() {
			GetTarget().SendEvent(m_event);
		}
	}
}
