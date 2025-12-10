using System;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Operations {
	[Serializable]
	public abstract class BehaviourOperation {
		public virtual string Editor_Info => string.Empty;

		internal virtual void Execute(BehaviourOwner owner) {
			OnExecute();
		}
		protected abstract void OnExecute();
	}

	public abstract class BehaviourOperation<T> : BehaviourOperation {
		[SerializeField] private BlackboardRef<T> m_target;

		protected string Editor_TargetInfo => m_target.IsVariable ? m_target.Editor_Info : "SELF";

		protected T GetTarget() => m_target.GetValue();

		internal override void Execute(BehaviourOwner owner) {
			if (!owner.SetTargetIfNeeded(ref m_target)) {
				Debug.LogError($"{nameof(BehaviourOperation)} is missing its {typeof(T).Name} target");
				return;
			}

			base.Execute(owner);
		}
	}
}
