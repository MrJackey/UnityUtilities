using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Jackey.Behaviours.BT.Nested {
	[GraphIcon("BehaviourTree")]
	[SearchPath("Nested/Behaviour Tree")]
	public class NestedBehaviourTree : BehaviourAction {
		[SerializeField] private BlackboardRef<BehaviourTree> m_behaviour;

		private BehaviourTree m_behaviourInstance;

		public BehaviourTree InstanceOrBehaviour => m_behaviourInstance != null ? m_behaviourInstance : m_behaviour.GetValue();

#if UNITY_EDITOR
		public override string Editor_Info {
			get {
				if (m_behaviour.IsVariable)
					return m_behaviour.Editor_Info;

				BehaviourTree value = m_behaviour.GetValue();
				if (value == null)
					return "<b>NONE</b>";

				return value.name;
			}
		}
#endif

		protected override ExecutionStatus OnEnter() {
			BehaviourTree behaviour = m_behaviour.GetValue();
			if (behaviour == null) {
				Debug.LogWarning($"Nested BehaviourTree in {m_runtimeBehaviour.name} does not have an assigned behaviour", Owner);
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
			if (m_behaviourInstance.Status == ActionStatus.Inactive) {
				m_behaviourInstance.Start();
				return (ExecutionStatus)m_behaviourInstance.Status;
			}

			return m_behaviourInstance.Tick();
		}

		protected override void OnInterrupt() {
			m_behaviourInstance.Stop();
		}

		protected override void OnExit() {
			DisableTicking();

			if (m_behaviourInstance != null)
				m_behaviourInstance.Stop();
		}
	}
}
