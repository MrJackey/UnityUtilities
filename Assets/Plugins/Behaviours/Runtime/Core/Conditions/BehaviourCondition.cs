using System;

namespace Jackey.Behaviours.Core.Conditions {
	[Serializable]
	public abstract class BehaviourCondition {
		public virtual string Editor_Info => string.Empty;

		public abstract bool Evaluate();
	}
}
