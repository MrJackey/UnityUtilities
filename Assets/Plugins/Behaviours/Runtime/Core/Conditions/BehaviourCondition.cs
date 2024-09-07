using System;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours.Core.Conditions {
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

		protected string TargetInfo => m_target.IsReferencingVariable ? m_target.Editor_Info : "SELF";

		protected T GetTarget() => m_target.GetValue();

		internal override void Enable(BehaviourOwner owner) {
			if (m_target.GetValue() == null) {
				if (!owner.TryGetComponent(out T target)) {
					Debug.LogError($"{nameof(BehaviourCondition)} is missing its {typeof(T).Name} target");
					return;
				}

				m_target.SetValue(target);
			}

			base.Enable(owner);
		}
	}
}
