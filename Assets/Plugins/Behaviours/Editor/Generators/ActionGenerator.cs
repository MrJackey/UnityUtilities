using System;
using System.IO;
using System.Text;
using Jackey.Behaviours.BT.Actions;
using UnityEditor;
using UnityEngine;

namespace Jackey.Behaviours.Editor.Generators {
	public static class ActionGenerator {
		private const string GENERATED_FILE_NAME = "BehaviourActions_Generated";

		private const string CLASS_TEMPLATE = @"using Jackey.Behaviours.Core;
using Jackey.Behaviours.Attributes;
using UnityEngine;

namespace Jackey.Behaviours.BT.Generated {{
	public class BehaviourActions_Generated {{
{0}
	}}
}}";

		private const string BASE_ACTION_TEMPLATE = @"
		[SearchPath(""Generated/{0}"")]
		public class {1} : BehaviourAction<{2}> {{
			protected override ExecutionStatus OnEnter() => GetTarget().OnEnter();
			protected override ExecutionStatus OnTick() => GetTarget().OnTick();
			protected override void OnInterrupt() => GetTarget().OnInterrupt();
			protected override void OnResult(ActionResult result) => GetTarget().OnResult(result);
			protected override void OnExit() => GetTarget().OnExit();
		}}
";

		private const string ARGS_ACTION_TEMPLATE = @"
		[SearchPath(""Generated/{0}"")]
		public class {1} : BehaviourAction<{2}> {{
			[SerializeField] private {3} m_args;

			protected override ExecutionStatus OnEnter() => GetTarget().OnEnter(m_args);
			protected override ExecutionStatus OnTick() => GetTarget().OnTick(m_args);
			protected override void OnInterrupt() => GetTarget().OnInterrupt(m_args);
			protected override void OnResult(ActionResult result) => GetTarget().OnResult(m_args, result);
			protected override void OnExit() => GetTarget().OnExit(m_args);
		}}
";

		[MenuItem("Tools/Jackey/Behaviours/Generate Actions")]
		public static void Regenerate() {
			string assetPath;

			// Prefer opening folder of existing asset
			string[] assetSearch = AssetDatabase.FindAssets($"t:Script {GENERATED_FILE_NAME}");
			if (assetSearch.Length > 0) {
				assetPath = AssetDatabase.GUIDToAssetPath(assetSearch[0]);
			}
			else {
				string userPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", null);

				if (string.IsNullOrEmpty(userPath))
					return;

				assetPath = Path.Join(userPath, $"{GENERATED_FILE_NAME}.cs");
				string relativePath = Path.GetRelativePath(Application.dataPath, assetPath);

				if (!AssetDatabase.IsValidFolder("Assets/" + Path.GetDirectoryName(relativePath))) {
					EditorUtility.DisplayDialog("Object Behaviours", "Selected folder is not valid. Please try again", "Ok");
					return;
				}
			}

			Generate(assetPath);

			AssetDatabase.Refresh();
			EditorApplication.delayCall += EditorUtility.RequestScriptReload;
		}

		private static void Generate(string assetPath) {
			TypeCache.TypeCollection baseTypes = TypeCache.GetTypesDerivedFrom(typeof(IComponentAction));
			StringBuilder baseBuilder = new StringBuilder();

			foreach (Type type in baseTypes) {
				baseBuilder.AppendFormat(
					BASE_ACTION_TEMPLATE,
					type.Name,
					$"{type.FullName.Replace('.', '_')}_Generated",
					type.FullName
				);
			}

			TypeCache.TypeCollection argTypes = TypeCache.GetTypesDerivedFrom(typeof(IComponentAction<>));
			StringBuilder argsBuilder = new StringBuilder();
			foreach (Type type in argTypes) {
				foreach (Type @interface in type.GetInterfaces()) {
					if (!@interface.IsGenericType)
						continue;

					Type typeDef = @interface.GetGenericTypeDefinition();
					if (typeDef != typeof(IComponentAction<>))
						continue;

					Type typeArgs = @interface.GetGenericArguments()[0];
					argsBuilder.AppendFormat(
						ARGS_ACTION_TEMPLATE,
						$"{type.Name}<{typeArgs.Name}>",
						$"{type.FullName.Replace('.', '_')}_{typeArgs.FullName.Replace('.', '_')}_Generated",
						type.FullName,
						typeArgs.FullName
					);
				}
			}

			string output = string.Format(CLASS_TEMPLATE, baseBuilder.AppendLine(argsBuilder.ToString()));
			File.WriteAllText(assetPath, output);
		}
	}
}
