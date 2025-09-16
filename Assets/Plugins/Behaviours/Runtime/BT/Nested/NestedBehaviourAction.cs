using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Actions;
using Jackey.Behaviours.Variables;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Jackey.Behaviours.BT.Nested {
	[GraphIcon("BehaviourTree")]
	[SearchPath("Utilities/Nested Behaviour")]
	public class NestedBehaviourAction : BehaviourAction {
		[SerializeField] private BlackboardRef<ObjectBehaviour> m_behaviour;

		private ObjectBehaviour m_behaviourInstance;

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
				Debug.LogWarning($"Nested Behaviour Action in {m_runtimeBehaviour.name} does not have an assigned behaviour", Owner);
				return ExecutionStatus.Failure;
			}

			if (m_behaviourInstance == null) {
				m_behaviourInstance = Object.Instantiate(behaviour);
				Owner.Blackboard.MergeInto(m_behaviourInstance.Blackboard);
				m_behaviourInstance.Initialize(Owner);
			}

			EnableTicking();
			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_behaviourInstance.Status == BehaviourStatus.Inactive) {
				m_behaviourInstance.Start();
				return (ExecutionStatus)m_behaviourInstance.Status;
			}

			return m_behaviourInstance.Tick();
		}

		protected override void OnExit() {
			DisableTicking();

			if (m_behaviourInstance != null)
				m_behaviourInstance.Stop();
		}
	}
}
