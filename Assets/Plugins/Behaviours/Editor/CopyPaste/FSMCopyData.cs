using System;

namespace Jackey.Behaviours.Editor.CopyPaste {
	[Serializable]
	public class FSMCopyData {
		public string[] StateTypes;
		public string[] States;
		public int[] TransitionIndices;
	}
}
