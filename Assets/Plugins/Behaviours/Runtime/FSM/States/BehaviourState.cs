using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[Serializable]
	public abstract class BehaviourState {
#if UNITY_EDITOR
		[SerializeField] internal string Name;
#endif

		[SerializeField] private List<StateTransition> m_transitions = new();

		protected StateMachine m_runtimeBehaviour;

		public BehaviourStatus Status { get; private set; } = BehaviourStatus.Inactive;
		public bool IsFinished => Status is BehaviourStatus.Success or BehaviourStatus.Failure;

		protected BehaviourOwner Owner => m_runtimeBehaviour.Owner;

		/// <summary>
		/// Should this state be ticked whenever it's behaviour is ticked.
		/// </summary>
		/// <remarks>
		/// If the behaviour is starting, the first tick will occur after the first behaviour tick.<br/>
		/// If the behaviour is being ticked, the first tick will occur at the next behaviour tick
		/// </remarks>
		protected internal virtual bool ShouldTick { get; }

		internal List<StateTransition> Transitions => m_transitions;

#if UNITY_EDITOR
		[SerializeField] internal EditorData Editor_Data = new();
#endif
		public virtual string Editor_Info => string.Empty;

		internal virtual void Initialize(StateMachine behaviour) {
			m_runtimeBehaviour = behaviour;
		}

		private ExecutionStatus Enter() {
			if (Status != BehaviourStatus.Inactive)
				Reset();

#if UNITY_EDITOR
			if (Editor_Data.Breakpoint) {
				Debug.Log($"Behaviour Breakpoint @{Owner.name}", Owner);
				UnityEditor.EditorApplication.isPaused = true;
			}
#endif

			ExecutionStatus enterStatus = OnEnter();
			Status = (BehaviourStatus)enterStatus;

			return enterStatus;
		}
		protected virtual ExecutionStatus OnEnter() => ExecutionStatus.Running;

		private ExecutionStatus Tick() {
			ExecutionStatus tickStatus = OnTick();
			Status = (BehaviourStatus)tickStatus;

			return tickStatus;
		}
		protected virtual ExecutionStatus OnTick() => ExecutionStatus.Running;

		internal void Interrupt() {
			Status = BehaviourStatus.Failure;

			OnInterrupt();
			Exit();
		}

		/// <summary>
		/// States are interrupted when a Tick transition evaluates to true
		/// <br/>
		/// <see cref="OnExit"/> is invoked directly afterwards
		/// </summary>
		protected virtual void OnInterrupt() { }

		private void Exit() {
			OnExit();
		}
		protected virtual void OnExit() { }

		internal bool CheckTransitions(StateTransitionContext ctx, out BehaviourState destination) {
			foreach (StateTransition transition in m_transitions) {
				if (transition.Evaluate(ctx, out destination))
					return true;
			}

			destination = null;
			return false;
		}

		internal void Reset() {
			Status = BehaviourStatus.Inactive;
			OnReset();
		}
		protected virtual void OnReset() { }

		internal ExecutionStatus EnterSequence() {
			if (IsFinished)
				Reset();

			ExecutionStatus enterStatus = Enter();

			if (IsFinished) {
				Exit();
			}

			return enterStatus;
		}

		internal ExecutionStatus TickSequence() {
			ExecutionStatus tickStatus = Tick();

			if (IsFinished) {
				Exit();
			}

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
