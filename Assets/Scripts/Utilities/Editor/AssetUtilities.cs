using System;
using JetBrains.Annotations;
using UnityEditor;
using Object = UnityEngine.Object;

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

		/// <inheritdoc cref="GetScriptAsset"/>
		[CanBeNull]
		public static MonoScript GetScriptAsset<T>() => GetScriptAsset(typeof(T));

		/// <summary>
		/// Get the asset associated to a specific script type. If the script has multiple types,
		/// the asset is only returned if it's the first one.
		/// </summary>
		/// <param name="type">The script to search for</param>
		/// <returns>The asset associated to the given script type. Returns null if none is found</returns>
		[CanBeNull]
		public static MonoScript GetScriptAsset(Type type) {
			foreach (MonoScript script in MonoImporter.GetAllRuntimeMonoScripts()) {
				if (script.GetClass() == type)
					return script;
			}

			return null;
		}
	}
}
