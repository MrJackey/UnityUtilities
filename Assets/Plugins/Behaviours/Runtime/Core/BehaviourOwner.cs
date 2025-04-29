using System;
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
		[SerializeField] private StartOption m_startMode;
		[SerializeField] private UpdateOption m_updateMode;
		[SerializeField] private RepeatOption m_repeatMode;

		private List<IBehaviourEventListener> m_eventListeners = new();

		private bool m_isRunning;

		public ObjectBehaviour Behaviour => m_behaviour;
		public Blackboard Blackboard => m_blackboard;

		public StartOption StartMode {
			get => m_startMode;
			set => m_startMode = value;
		}

		public UpdateOption UpdateMode {
			get => m_updateMode;
			set => m_updateMode = value;
		}

		public RepeatOption RepeatMode {
			get => m_repeatMode;
			set => m_repeatMode = value;
		}

		private void Start() {
			if (m_startMode != StartOption.Start) return;

			if (m_behaviour == null) {
				Debug.LogWarning($"BehaviourOwner \"{name}\" is set to start but is missing a behaviour", this);
				return;
			}

			SetRuntimeBehaviourInstance(Instantiate(m_behaviour));
			StartBehaviour();
		}

		private void Update() {
			if (!m_isRunning) return;
			if (m_updateMode != UpdateOption.Update) return;

			TickBehaviour();
		}

		private void FixedUpdate() {
			if (!m_isRunning) return;
			if (m_updateMode != UpdateOption.FixedUpdate) return;

			TickBehaviour();
		}

		private void LateUpdate() {
			if (!m_isRunning) return;
			if (m_updateMode != UpdateOption.LateUpdate) return;

			TickBehaviour();
		}

		private void OnDestroy() {
			if (m_behaviour != null) {
				m_behaviour.Stop();
				m_isRunning = false;
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Directly set the behaviour field without any initialization done.
		/// </summary>
		/// <remarks>
		///	This method is only available in the editor. For runtime, see <see cref="SetRuntimeBehaviour"/> and <see cref="SetRuntimeBehaviourInstance"/>
		/// </remarks>
		public void Editor_SetBehaviour(ObjectBehaviour behaviour) {
			m_behaviour = behaviour;
		}
#endif

		/// <summary>
		/// Set the runtime behaviour of the object. This will create a new instance of the input behaviour
		/// and initialize it with this as its owner.<br/>
		/// Use this if you are assigning an asset
		/// </summary>
		public void SetRuntimeBehaviour(ObjectBehaviour behaviour) => SetRuntimeBehaviourInstance(Instantiate(behaviour));

		/// <summary>
		/// Set the runtime behaviour of the object. This will also initialize it with this as its owner.
		/// Use this only if you already have created an instance yourself. Otherwise use <see cref="SetRuntimeBehaviour"/>
		/// </summary>
		public void SetRuntimeBehaviourInstance(ObjectBehaviour instance) {
			m_behaviour = instance;

			m_blackboard.MergeInto(m_behaviour.Blackboard);
			m_behaviour.Initialize(this);
		}

		/// <summary>
		/// Create a new instance of the assigned behaviour, initialize it and start it directly afterwards.
		/// If you want to start an already assigned runtime behaviour, use <see cref="StartBehaviour"/> instead
		/// </summary>
		public void InitializeAndStartBehaviour() {
			SetRuntimeBehaviour(m_behaviour);
			StartBehaviour();
		}

		public void StartBehaviour() {
			m_behaviour.Start();
			m_isRunning = true;
		}

		public void TickBehaviour() {
			ExecutionStatus tickStatus = m_behaviour.Tick();

			if (tickStatus != ExecutionStatus.Running) {
				StopBehaviour();

				if (m_repeatMode == RepeatOption.Repeat)
					StartBehaviour();
			}
		}

		public void StopBehaviour() {
			m_behaviour.Stop();
			m_isRunning = false;
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

		internal bool SetTargetIfNeeded<T>(ref BlackboardRef<T> target) {
			if (target.IsVariable)
				return true;

			Type targetType = typeof(T);
			if (!targetType.IsInterface && !typeof(Component).IsAssignableFrom(targetType))
				return true;

			if (target.GetValue() != null)
				return true;

			if (!TryGetComponent(out T component))
				return false;

			target.SetValue(component);
			return true;
		}

		public enum StartOption {
			Start,
			Manual,
		}

		public enum UpdateOption {
			Update,
			LateUpdate,
			FixedUpdate,
			Manual,
		}

		public enum RepeatOption {
			Repeat,
			Stop,
		}
	}
}
