using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Jackey.Behaviours.Editor.Utilities {
	public static class SerializationUtilities {
		private const string SERIALIZED_MANAGED_TYPE_FORMAT = "{{class: ({0}), ns: ({1}), asm: ({2})}}";

		public static void RepairMissingManagedTypes(Object asset, ManagedReferenceMissingType missingType, string asm, string ns, string type) {
			string assetPath = AssetDatabase.GetAssetPath(asset);
			string projectPath = Path.GetDirectoryName(Application.dataPath);
			string absoluteAssetPath = Path.Join(projectPath, assetPath);

			string replacePattern = string.Format(SERIALIZED_MANAGED_TYPE_FORMAT, missingType.className, missingType.namespaceName, missingType.assemblyName);

			string assetContent = File.ReadAllText(absoluteAssetPath);
			string repairedAsset = Regex.Replace(assetContent, replacePattern, _ => $"{{class: {type}, ns: {ns}, asm: {asm}}}");
			File.WriteAllText(absoluteAssetPath, repairedAsset);
		}
	}
}
