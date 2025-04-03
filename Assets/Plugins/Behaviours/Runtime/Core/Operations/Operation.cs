using System;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours.Core.Operations {
	[Serializable]
	public abstract class Operation {
		public virtual string Editor_Info => string.Empty;

		internal virtual void Execute(BehaviourOwner owner) {
			OnExecute();
		}
		protected abstract void OnExecute();
	}

	public abstract class Operation<T> : Operation {
		[SerializeField] private BlackboardRef<T> m_target;

		protected string TargetInfo => m_target.IsVariable ? m_target.Editor_Info : "SELF";
		protected T GetTarget() => m_target.GetValue();

		internal override void Execute(BehaviourOwner owner) {
			if (!owner.SetTargetIfNeeded(ref m_target)) {
				Debug.LogError($"{nameof(Operation)} is missing its {typeof(T).Name} target");
				return;
			}

			base.Execute(owner);
		}
	}
}
