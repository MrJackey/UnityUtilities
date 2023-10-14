using System;

namespace Jackey.SelectionHistory.Editor {
	[Flags]
	public enum SelectionTypes {
		Assets = 1 << 0,
		SceneObjects = 1 << 1,
	}
}
