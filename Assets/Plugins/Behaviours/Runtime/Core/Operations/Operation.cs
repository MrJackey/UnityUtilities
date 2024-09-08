using System;

namespace Jackey.Behaviours.Core.Operations {
	[Serializable]
	public abstract class Operation {
		public virtual string Editor_Info => string.Empty;

		public abstract void Execute();
	}
}
