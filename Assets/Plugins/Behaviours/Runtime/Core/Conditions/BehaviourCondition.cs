using System;

namespace Jackey.Behaviours.Core.Conditions {
	[Serializable]
	public abstract class BehaviourCondition {
		public virtual string Editor_Info => string.Empty;

		public virtual void OnEnable(BehaviourOwner owner) { }
		public abstract bool Evaluate();
		public virtual void OnDisable() { }
	}
}
