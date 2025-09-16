using System;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Conditions {
	[Serializable]
	public abstract class BehaviourCondition {
		public virtual string Editor_Info => string.Empty;

		internal virtual void Enable(BehaviourOwner owner) {
			OnEnable(owner);
		}
		protected virtual void OnEnable(BehaviourOwner owner) { }
		public abstract bool Evaluate();
		public virtual void OnDisable() { }
	}

	public abstract class BehaviourCondition<T> : BehaviourCondition {
		[SerializeField] private BlackboardRef<T> m_target;

		protected string Editor_TargetInfo => m_target.IsVariable ? m_target.Editor_Info : "SELF";

		protected T GetTarget() => m_target.GetValue();

		internal override void Enable(BehaviourOwner owner) {
			if (!owner.SetTargetIfNeeded(ref m_target)) {
				Debug.LogError($"{nameof(BehaviourCondition)} is missing its {typeof(T).Name} target");
				return;
			}

			base.Enable(owner);
		}
	}
}
