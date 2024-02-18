using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours {
	public abstract class ObjectBehaviour : ScriptableObject {
		[SerializeField] internal Blackboard m_blackboard;

		protected internal BehaviourOwner Owner { get; internal set; }
		public Blackboard Blackboard => m_blackboard;

		internal virtual void Initialize(BehaviourOwner owner) {
			Owner = owner;
		}

		internal abstract void Start();
		internal abstract ExecutionStatus Tick();
		internal abstract void Stop();
	}
}
