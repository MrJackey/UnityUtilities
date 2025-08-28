using System.Collections.Generic;
using Jackey.Behaviours.FSM.States;
using UnityEngine;

namespace Jackey.Behaviours.FSM {
	[CreateAssetMenu(fileName = "new StateMachine", menuName = "Jackey/Behaviour/State Machine", order = 0)]
	public class StateMachine : ObjectBehaviour {
		[SerializeReference] internal List<BehaviourState> m_allStates = new();
		[SerializeReference] internal BehaviourState m_entry;

		private BehaviourState m_activeState;
		private bool m_deferActiveStateTick;

		internal override void Initialize(BehaviourOwner owner) {
			if (m_entry == null) {
				Debug.LogError("State Machine does not have an entry state. Unable to initialize", this);
				return;
			}

			base.Initialize(owner);

			foreach (BehaviourState state in m_allStates) {
				state.Initialize(this);
			}
		}

		internal override void Start() {
			if (Status != BehaviourStatus.Inactive)
				return;

			m_activeState = m_entry;
			m_deferActiveStateTick = true;
		}

		internal override ExecutionStatus Tick() {
			bool deferTick = m_deferActiveStateTick;
			m_deferActiveStateTick = false;

			while (true) {
				BehaviourState destination;

				// Check Tick transitions
				if (m_activeState.CheckTransitions(StateTransitionContext.OnTick, out destination)) {
					m_activeState.Interrupt();
					m_activeState = destination;

					return Traverse(out destination);
				}

				if (deferTick || !m_activeState.ShouldTick)
					return ExecutionStatus.Running;

				ExecutionStatus tickStatus = m_activeState.TickSequence();
				if (tickStatus == ExecutionStatus.Running)
					return ExecutionStatus.Running;

				// Check Finish transitions
				StateTransitionContext finishCtx = tickStatus == ExecutionStatus.Success ? StateTransitionContext.OnSuccess : StateTransitionContext.OnFailure;
				if (m_activeState.CheckTransitions(finishCtx, out destination)) {
					m_activeState = destination;

					return Traverse(out destination);
				}

				return tickStatus; // Success || Failure
			}
		}

		private ExecutionStatus Traverse(out BehaviourState destination) {
			destination = null;

			// Continue until running / end of fsm
			ExecutionStatus enterStatus = m_activeState.EnterSequence();
			while (enterStatus != ExecutionStatus.Running) {
				StateTransitionContext enterCtx = enterStatus == ExecutionStatus.Success ? StateTransitionContext.OnSuccess : StateTransitionContext.OnFailure;

				if (m_activeState.CheckTransitions(enterCtx, out destination)) {
					m_activeState = destination;
					enterStatus = m_activeState.EnterSequence();
				}
				else {
					return enterStatus; // Success || Failure
				}
			}

			return ExecutionStatus.Running;
		}

		internal override void Stop() {
			m_activeState.Interrupt();

			foreach (BehaviourState state in m_allStates) {
				if (state.IsFinished)
					state.Reset();
			}

			Status = BehaviourStatus.Inactive;
		}

		private void Reset() {
			m_entry = null;
			m_allStates?.Clear();
		}

#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();

			if (UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(this))
				return;

			if (m_entry != null && !m_allStates.Contains(m_entry))
				m_entry = null;

			foreach (BehaviourState state in m_allStates) {
				ConnectBlackboardRefs(state);
			}
		}
#endif
	}
}
