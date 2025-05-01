using System;
using System.IO;
using System.Text.RegularExpressions;
using Jackey.Behaviours.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.Behaviours.Editor.Utilities {
	public static class SerializationUtilities {
		private const string SERIALIZED_MANAGED_TYPE_FORMAT = @"{{class: ({0}),\s+ns: ({1}),\s+asm: ({2})}}";
		private const string NULL_MANAGED_ENTRY_PATTERN = @"^\s+- rid: -2\n(?!\s+type: {class: , ns: , asm: })";

		public static void RepairMissingManagedTypes(Object asset, ManagedReferenceMissingType missingType, string asm, string ns, string cls) {
			string assetPath = AssetDatabase.GetAssetPath(asset);
			string projectPath = Path.GetDirectoryName(Application.dataPath);
			string absoluteAssetPath = Path.Join(projectPath, assetPath);

			string replacePattern = string.Format(SERIALIZED_MANAGED_TYPE_FORMAT, missingType.className, missingType.namespaceName, missingType.assemblyName);

			string assetContent = File.ReadAllText(absoluteAssetPath);
			string repairedContent = Regex.Replace(assetContent, replacePattern, _ => $"{{class: {cls}, ns: {ns}, asm: {asm}}}");
			File.WriteAllText(absoluteAssetPath, repairedContent);
		}

		public static void RemoveNullManagedTypes(Object asset) {
			string assetPath = AssetDatabase.GetAssetPath(asset);
			string projectPath = Path.GetDirectoryName(Application.dataPath);
			string absoluteAssetPath = Path.Join(projectPath, assetPath);

			string assetContent = File.ReadAllText(absoluteAssetPath);
			string removedContent = Regex.Replace(assetContent, NULL_MANAGED_ENTRY_PATTERN, string.Empty, RegexOptions.Multiline);
			File.WriteAllText(absoluteAssetPath, removedContent);
		}

		public static T DeepClone<T>(T original) where T : BehaviourAction {
			if (original == null)
				return default;

			string json = JsonUtility.ToJson(original);
			T clone = (T)Activator.CreateInstance(original.GetType());
			JsonUtility.FromJsonOverwrite(json, clone);

			return clone;
		}
	}
}
