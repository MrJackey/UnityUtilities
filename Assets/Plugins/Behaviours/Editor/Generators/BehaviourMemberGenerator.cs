using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
			protected override ExecutionStatus OnEnter() => GetTarget().OnEnter(this);
			protected override ExecutionStatus OnTick() => GetTarget().OnTick(this);
			protected override void OnInterrupt() => GetTarget().OnInterrupt(this);
			protected override void OnResult(ActionResult result) => GetTarget().OnResult(this, result);
			protected override void OnExit() => GetTarget().OnExit(this);
		}}
";

		private const string ARGS_ACTION_TEMPLATE = @"
		[DisplayName(""{0}"")]
		[SearchPath(""Generated/{0}"")]
		public sealed class {1} : BehaviourAction<{2}> {{
			[SerializeField] private BlackboardRef<{3}> m_args;

			protected override ExecutionStatus OnEnter() => GetTarget().OnEnter(this, m_args.GetValue());
			protected override ExecutionStatus OnTick() => GetTarget().OnTick(this, m_args.GetValue());
			protected override void OnInterrupt() => GetTarget().OnInterrupt(this, m_args.GetValue());
			protected override void OnResult(ActionResult result) => GetTarget().OnResult(this, m_args.GetValue(), result);
			protected override void OnExit() => GetTarget().OnExit(this, m_args.GetValue());
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
					$"{FullNameToClassName(type.FullName)}_Generated", // Class Name
					FullNameToMemberType(type.FullName) // Target Arg
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
						$"{FullNameToClassName(type.FullName)}_{FullNameToClassName(typeArgs.FullName)}_Generated", // Class Name
						FullNameToMemberType(type.FullName), // Target Arg
						FullNameToMemberType(typeArgs.FullName) // Serialized Arg Type
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
						$"{method.DeclaringType.Name}.{method.Name}()", // Condition Name
						$"{FullNameToClassName(method.DeclaringType.FullName)}_{method.Name}_Generated", // Class Name
						FullNameToMemberType(method.DeclaringType.FullName), // Target Arg
						$"{method.Name}" // Method Call
					);
				}
				else {
					builder.AppendFormat(
						ARGS_CONDITION_TEMPLATE,
						$"{method.DeclaringType.Name}.{method.Name}({parameters[0].ParameterType.Name})", // Name
						$"{FullNameToClassName(method.DeclaringType.FullName)}_{method.Name}_{parameters[0].ParameterType.Name}_Generated", // ClassName
						FullNameToMemberType(method.DeclaringType.FullName), // Target Arg
						FullNameToMemberType(parameters[0].ParameterType.FullName), // Serialized Arg Type
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
						$"{method.DeclaringType.Name}.{method.Name}()", // Name
						$"{FullNameToClassName(method.DeclaringType.FullName)}_{method.Name}_Generated", // Class Name
						FullNameToMemberType(method.DeclaringType.FullName), // Target Arg
						$"{method.Name}" // Method Call
					);
				}
				else {
					builder.AppendFormat(
						ARGS_OPERATION_TEMPLATE,
						$"{method.DeclaringType.Name}.{method.Name}({parameters[0].ParameterType.Name})", // Name
						$"{FullNameToClassName(method.DeclaringType.FullName)}_{method.Name}_{parameters[0].ParameterType.Name}_Generated", // Class Name
						FullNameToMemberType(method.DeclaringType.FullName), // Target Arg
						FullNameToMemberType(parameters[0].ParameterType.FullName), // Serialized Arg Type
						$"{method.Name}" // Method Call
					);
				}
			}

			return builder;
		}

		private static string FullNameToClassName(string fullName) {
			return Regex.Replace(fullName, @"\.|\+", "_");
		}

		private static string FullNameToMemberType(string fullName) {
			return Regex.Replace(fullName, @"\+", ".");
		}
	}
}
