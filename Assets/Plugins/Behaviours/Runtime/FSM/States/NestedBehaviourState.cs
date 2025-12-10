using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.FSM.States {
	[GraphIcon("BehaviourTree")]
	[SearchPath("Nested Behaviour State")]
	public class NestedBehaviourState : BehaviourState {
		[SerializeField] private BlackboardRef<ObjectBehaviour> m_behaviour;

		private ObjectBehaviour m_behaviourInstance;

		protected internal override bool ShouldTick => true;

		public ObjectBehaviour InstanceOrBehaviour => m_behaviourInstance != null ? m_behaviourInstance : m_behaviour.GetValue();

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (m_behaviour.IsVariable)
					return m_behaviour.Editor_Info;

				ObjectBehaviour value = m_behaviour.GetValue();
				if (value == null)
					return "<b>NONE</b>";

				return value.name;
			}
		}
#endif

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
