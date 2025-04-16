using UnityEditor;

namespace Jackey.Behaviours.Editor.Utilities {
	public static class UndoUtilities {
		public static int CreateGroup(string name) {
			Undo.IncrementCurrentGroup();
			int group = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName(name);

			return group;
		}
	}
}
