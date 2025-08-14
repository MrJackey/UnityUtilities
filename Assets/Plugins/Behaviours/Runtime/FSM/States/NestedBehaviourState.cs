using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	public class NestedBehaviourState : BehaviourState {
		[SerializeField] private BlackboardRef<ObjectBehaviour> m_behaviour;

		private ObjectBehaviour m_behaviourInstance;

		protected override ExecutionStatus OnEnter() {
			ObjectBehaviour behaviour = m_behaviour.GetValue();
			if (behaviour == null) {
				Debug.LogWarning($"Nested State in {m_runtimeBehaviour.name} does not have an assigned behaviour", Owner);
				return ExecutionStatus.Failure;
			}

			if (m_behaviourInstance == null) {
				m_behaviourInstance = Object.Instantiate(behaviour);
				Owner.Blackboard.MergeInto(m_behaviourInstance.Blackboard);
				m_behaviourInstance.Initialize(Owner);
			}

			m_behaviourInstance.Start();
			return (ExecutionStatus)m_behaviourInstance.Status;
		}

		protected override ExecutionStatus OnTick() {
			return m_behaviourInstance.Tick();
		}

		protected override void OnExit() {
			if (m_behaviourInstance != null)
				m_behaviourInstance.Stop();
		}
	}
}
