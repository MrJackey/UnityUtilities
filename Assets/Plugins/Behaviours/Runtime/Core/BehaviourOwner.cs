using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Core.Events;
using UnityEngine;

namespace Jackey.Behaviours {
	[BehaviourType]
	public class BehaviourOwner : MonoBehaviour {
		[SerializeField] private ObjectBehaviour m_behaviour;
		[SerializeField] private Blackboard m_blackboard;

		[Space]
		[SerializeField] private StartMode m_startMode;
		[SerializeField] private UpdateMode m_updateMode;
		[SerializeField] private RepeatMode m_repeatMode;

		private List<IBehaviourEventListener> m_eventListeners = new();

		public ObjectBehaviour Behaviour => m_behaviour;
		public Blackboard Blackboard => m_blackboard;

		private void Awake() {
			if (m_behaviour == null && m_updateMode != UpdateMode.Manual) {
				enabled = false;
			}
		}

		private void Start() {
			SetBehaviourInstance(Instantiate(m_behaviour));

			if (m_startMode == StartMode.Start)
				StartBehaviour();
		}

		private void Update() {
			if (m_updateMode != UpdateMode.Update) return;

			TickBehaviour();
		}

		private void FixedUpdate() {
			if (m_updateMode != UpdateMode.FixedUpdate) return;

			TickBehaviour();
		}

		private void LateUpdate() {
			if (m_updateMode != UpdateMode.LateUpdate) return;

			TickBehaviour();
		}

		public void SetBehaviour(ObjectBehaviour behaviour) => SetBehaviourInstance(Instantiate(behaviour));
		public void SetBehaviourInstance(ObjectBehaviour instance) {
			m_behaviour = instance;

			m_blackboard.MergeInto(m_behaviour.Blackboard);
			m_behaviour.Initialize(this);
		}

		public void StartBehaviour() {
			m_behaviour.Start();
		}

		public void TickBehaviour() {
			ExecutionStatus tickStatus = m_behaviour.Tick();

			if (tickStatus != ExecutionStatus.Running) {
				StopBehaviour();

				if (m_repeatMode == RepeatMode.Repeat)
					StartBehaviour();
			}
		}

		public void StopBehaviour() {
			m_behaviour.Stop();
		}

		public void AddEventListener(IBehaviourEventListener listener) {
			m_eventListeners.Add(listener);
		}

		public void RemoveEventListener(IBehaviourEventListener listener) {
			m_eventListeners.Remove(listener);
		}

		public void SendEvent(BehaviourEvent evt) {
			int listenerCount = m_eventListeners.Count;
			for (int i = 0; i < listenerCount; i++) {
				m_eventListeners[i].OnEvent(evt);
			}
		}

		private enum StartMode {
			Start,
			Manual,
		}

		private enum UpdateMode {
			Update,
			LateUpdate,
			FixedUpdate,
			Manual,
		}

		private enum RepeatMode {
			Repeat,
			Stop,
		}
	}
}
