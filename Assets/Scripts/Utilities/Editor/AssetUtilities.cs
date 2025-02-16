using UnityEditor;
using UnityEngine;

namespace Jackey.Utilities.Editor {
	public static class AssetUtilities {
		/// <summary>
		/// Shortcut for converting a GUID to an asset path and immediately loading the asset
		/// </summary>
		/// <param name="guid">The GUID of the asset</param>
		/// <returns>The asset with the input GUID</returns>
		public static T LoadAssetWithGUID<T>(string guid) where T : Object {
			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
		}
	}
}
