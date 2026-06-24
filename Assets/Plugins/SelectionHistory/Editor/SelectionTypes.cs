using System;

namespace Jackey.SelectionHistory.Editor {
	[Flags]
	public enum SelectionTypes {
		Assets = 1 << 0,
		Folders = 1 << 1,
		SceneObjects = 1 << 2,
	}
}
