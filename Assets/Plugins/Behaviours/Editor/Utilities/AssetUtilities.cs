using System;
using JetBrains.Annotations;
using UnityEditor;

namespace Jackey.Behaviours.Editor.Utilities {
	public static class AssetUtilities {
		[CanBeNull]
		public static MonoScript GetScriptAsset<T>() => GetScriptAsset(typeof(T));

		[CanBeNull]
		public static MonoScript GetScriptAsset(Type type) {
			foreach (MonoScript runtimeScript in MonoImporter.GetAllRuntimeMonoScripts()) {
				if (runtimeScript.GetClass() == type)
					return runtimeScript;
			}

			return null;
		}
	}
}
