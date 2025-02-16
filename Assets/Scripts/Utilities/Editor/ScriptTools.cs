using UnityEditor;

namespace Jackey.Utilities.Editor {
	public static class ScriptTools
	{
		[MenuItem("Tools/Jackey/Scripts/Reload Script Assemblies", false, 0)]
		private static void ReloadScriptAssemblies()
		{
			EditorUtility.RequestScriptReload();
		}
	}
}
