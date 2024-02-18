using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours {
	public class BehaviourOwner : MonoBehaviour {
		[SerializeField] private ObjectBehaviour m_behaviour;
		[SerializeField] private Blackboard m_blackBoard;

		[Space]
		[SerializeField] private StartMode m_startMode;
		[SerializeField] private UpdateMode m_updateMode;
		[SerializeField] private RepeatMode m_repeatMode;

		public ObjectBehaviour Behaviour => m_behaviour;

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

			m_blackBoard.MergeInto(m_behaviour.Blackboard);
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
