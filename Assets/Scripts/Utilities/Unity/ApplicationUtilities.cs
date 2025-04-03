using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Unity {
	public static class ApplicationUtilities {
		public static void Quit() {
#if UNITY_EDITOR
			EditorApplication.ExitPlaymode();
#else
			Application.Quit();
#endif
		}
	}
}
