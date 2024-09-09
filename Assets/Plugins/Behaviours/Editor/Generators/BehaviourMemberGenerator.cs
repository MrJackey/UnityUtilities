using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT.Actions;
using UnityEditor;
using UnityEngine;

namespace Jackey.Behaviours.Editor.Generators {
	public static class BehaviourMemberGenerator {
		private const string GENERATED_FILE_NAME = "BehaviourMembers_Generated";

		private const string CLASS_TEMPLATE = @"using Jackey.Behaviours.Core;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.Core.Operations;
using UnityEngine;

namespace Jackey.Behaviours.BT.Generated {{
	public sealed class BehaviourMembers_Generated {{
{0}
	}}
}}";

		private const string BASE_ACTION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : BehaviourAction<{2}> {{
			protected override ExecutionStatus OnEnter() => GetTarget().OnEnter();
			protected override ExecutionStatus OnTick() => GetTarget().OnTick();
			protected override void OnInterrupt() => GetTarget().OnInterrupt();
			protected override void OnResult(ActionResult result) => GetTarget().OnResult(result);
			protected override void OnExit() => GetTarget().OnExit();
		}}
";

		private const string ARGS_ACTION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : BehaviourAction<{2}> {{
			[SerializeField] private BlackboardRef<{3}> m_args;

			protected override ExecutionStatus OnEnter() => GetTarget().OnEnter(m_args.GetValue());
			protected override ExecutionStatus OnTick() => GetTarget().OnTick(m_args.GetValue());
			protected override void OnInterrupt() => GetTarget().OnInterrupt(m_args.GetValue());
			protected override void OnResult(ActionResult result) => GetTarget().OnResult(m_args.GetValue(), result);
			protected override void OnExit() => GetTarget().OnExit(m_args.GetValue());
		}}
";

		private const string BASE_CONDITION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : BehaviourCondition<{2}> {{ 
			public override bool Evaluate() => GetTarget().{3}();
		}}
";

		private const string ARGS_CONDITION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : BehaviourCondition<{2}> {{ 
			[SerializeField] private BlackboardRef<{3}> m_args;

			public override bool Evaluate() => GetTarget().{4}(m_args.GetValue());
		}}
";

		private const string BASE_OPERATION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : Operation<{2}> {{
			protected override void OnExecute() {{
				GetTarget().{3}();
			}}
		}}
";

		private const string ARGS_OPERATION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : Operation<{2}> {{
			[SerializeField] private BlackboardRef<{3}> m_args;
			
			protected override void OnExecute() {{
				GetTarget().{4}(m_args.GetValue());
			}}
		}}
";

		[MenuItem("Tools/Jackey/Behaviours/Generate Members")]
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
			StringBuilder baseBuilder = GenerateBaseActions();
			StringBuilder argsBuilder = GenerateArgActions();
			StringBuilder conditionsBuilder = GenerateConditions();
			StringBuilder operationsBuilder = GenerateOperations();

			string output = string.Format(
				CLASS_TEMPLATE,
				baseBuilder.Append(argsBuilder.ToString())
					.Append(conditionsBuilder.ToString())
					.Append(operationsBuilder.ToString())
			);
			File.WriteAllText(assetPath, output);
		}

		private static StringBuilder GenerateBaseActions() {
			StringBuilder builder = new StringBuilder();
			TypeCache.TypeCollection baseTypes = TypeCache.GetTypesDerivedFrom(typeof(IComponentAction));

			foreach (Type type in baseTypes) {
				builder.AppendFormat(
					BASE_ACTION_TEMPLATE,
					type.Name, // Action Name
					$"{type.FullName.Replace('.', '_')}_Generated", // Class Name
					type.FullName // Target Arg
				);
			}

			return builder;
		}

		private static StringBuilder GenerateArgActions() {
			StringBuilder builder = new StringBuilder();
			TypeCache.TypeCollection argTypes = TypeCache.GetTypesDerivedFrom(typeof(IComponentAction<>));

			foreach (Type type in argTypes) {
				foreach (Type @interface in type.GetInterfaces()) {
					if (!@interface.IsGenericType)
						continue;

					Type typeDef = @interface.GetGenericTypeDefinition();
					if (typeDef != typeof(IComponentAction<>))
						continue;

					Type typeArgs = @interface.GetGenericArguments()[0];
					builder.AppendFormat(
						ARGS_ACTION_TEMPLATE,
						$"{type.Name}<{typeArgs.Name}>", // Action Name
						$"{type.FullName.Replace('.', '_')}_{typeArgs.FullName.Replace('.', '_')}_Generated", // Class Name
						type.FullName, // Target Arg
						typeArgs.FullName // Serialized Arg Type
					);
				}
			}

			return builder;
		}

		private static StringBuilder GenerateConditions() {
			StringBuilder builder = new StringBuilder();
			IEnumerable<MethodInfo> methods = TypeCache.GetMethodsWithAttribute<BehaviourConditionAttribute>()
				.Where(methodInfo => methodInfo.IsPublic && methodInfo.ReturnType == typeof(bool) && methodInfo.GetParameters().Length <= 1);

			foreach (MethodInfo method in methods) {
				ParameterInfo[] parameters = method.GetParameters();

				if (parameters.Length == 0) {
					builder.AppendFormat(
						BASE_CONDITION_TEMPLATE,
						$"{method.DeclaringType.Name}.{method.Name}", // Condition Name
						$"{method.DeclaringType.FullName.Replace('.', '_')}_{method.Name}_Generated", // Class Name
						method.DeclaringType.FullName, // Type Arg
						$"{method.Name}" // Method Call
					);
				}
				else {
					builder.AppendFormat(
						ARGS_CONDITION_TEMPLATE,
						$"{method.DeclaringType.Name}.{method.Name}({parameters[0].ParameterType.Name})", // Name
						$"{method.DeclaringType.FullName.Replace('.', '_')}_{method.Name}_{parameters[0].ParameterType.Name}_Generated", // ClassName
						method.DeclaringType.FullName, // Target Arg
						parameters[0].ParameterType.FullName, // Serialized Arg Type
						$"{method.Name}" // Method Call
					);
				}
			}

			return builder;
		}

		private static StringBuilder GenerateOperations() {
			StringBuilder builder = new StringBuilder();
			IEnumerable<MethodInfo> methods = TypeCache.GetMethodsWithAttribute<BehaviourOperationAttribute>()
				.Where(methodInfo => methodInfo.IsPublic && methodInfo.ReturnType == typeof(void) && methodInfo.GetParameters().Length <= 1);

			foreach (MethodInfo method in methods) {
				ParameterInfo[] parameters = method.GetParameters();

				if (parameters.Length == 0) {
					builder.AppendFormat(
						BASE_OPERATION_TEMPLATE,
						$"{method.DeclaringType.Name}.{method.Name}", // Condition Name
						$"{method.DeclaringType.FullName.Replace('.', '_')}_{method.Name}_Generated", // Class Name
						method.DeclaringType.FullName, // Type Arg
						$"{method.Name}" // Method Call
					);
				}
				else {
					builder.AppendFormat(
						ARGS_OPERATION_TEMPLATE,
						$"{method.DeclaringType.Name}.{method.Name}({parameters[0].ParameterType.Name})", // Name
						$"{method.DeclaringType.FullName.Replace('.', '_')}_{method.Name}_{parameters[0].ParameterType.Name}_Generated", // ClassName
						method.DeclaringType.FullName, // Target Arg
						parameters[0].ParameterType.FullName, // Serialized Arg Type
						$"{method.Name}" // Method Call
					);
				}
			}

			return builder;
		}
	}
}
