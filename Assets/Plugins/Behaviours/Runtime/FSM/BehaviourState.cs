using System;
using System.Collections.Generic;
using Jackey.Behaviours.BT;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.Behaviours.FSM {
	[Serializable]
	public class BehaviourState {
		[SerializeField] private BehaviourTree m_stateBehaviour;
		// TODO: Figure out how transitions should really function
		[SerializeField] private List<StateTransition> m_transitions = new();

		protected StateMachine m_runtimeBehaviour;
		private BehaviourTree m_behaviourInstance;

		public ActionStatus Status { get; set; } = ActionStatus.Inactive;
		public bool IsFinished => Status is ActionStatus.Success or ActionStatus.Failure;

		protected BehaviourOwner Owner => m_runtimeBehaviour.Owner;

		public List<StateTransition> Transitions => m_transitions;

#if UNITY_EDITOR
		[SerializeField] internal EditorData Editor_Data = new();
#endif
		public virtual string Editor_Info => $"{(m_stateBehaviour != null ? m_stateBehaviour.name : "NONE")}";

		internal virtual void Initialize(StateMachine behaviour) {
			m_runtimeBehaviour = behaviour;
		}

		private ExecutionStatus Enter() {
#if UNITY_EDITOR
			if (Editor_Data.Breakpoint) {
				Debug.Log($"Behaviour Breakpoint @{Owner.name}", Owner);
				UnityEditor.EditorApplication.isPaused = true;
			}
#endif

			if (m_behaviourInstance == null) {
				m_behaviourInstance = Object.Instantiate(m_stateBehaviour);
				m_behaviourInstance.Initialize(Owner);
			}

			foreach (StateTransition transition in Transitions)
				transition.Enable(Owner);

			m_stateBehaviour.Start();
			Status = m_stateBehaviour.Status;

			return (ExecutionStatus)Status;
		}

		private ExecutionStatus Tick() {
			if (IsFinished)
				return (ExecutionStatus)Status;

			// TODO: Evaluate transitions before/after tick?

			ExecutionStatus tickStatus = m_behaviourInstance.Tick();
			Status = (ActionStatus)tickStatus;

			if (IsFinished)
				Exit();

			return tickStatus;
		}

		public void Interrupt() {
			Status = ActionStatus.Failure;
			Exit();
		}

		private void Exit() {
			m_behaviourInstance.Stop();
		}

		internal bool TryGetNextState(out BehaviourState nextState) {
			foreach (StateTransition transition in m_transitions) {
				if (transition.Evaluate(this)) {
					nextState = transition.To;
					return true;
				}
			}

			nextState = null;
			return false;
		}

		public ExecutionStatus EnterSequence() {
			ExecutionStatus enterStatus = Enter();

			if (IsFinished) {
				Exit();
				return enterStatus;
			}

			ExecutionStatus tickStatus = Tick();

			if (IsFinished)
				Exit();

			return tickStatus;
		}

		public ExecutionStatus TickSequence() {
			ExecutionStatus tickStatus = Tick();

			if (IsFinished)
				Exit();

			return tickStatus;
		}

#if UNITY_EDITOR
		[Serializable]
		internal class EditorData {
			public Vector2 Position;
			public bool Breakpoint;
		}
#endif
	}
}
