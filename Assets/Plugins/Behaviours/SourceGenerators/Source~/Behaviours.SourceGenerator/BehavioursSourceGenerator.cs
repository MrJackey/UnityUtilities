using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Jackey.Behaviours.SourceGenerators;

[Generator]
public class BehavioursSourceGenerator : IIncrementalGenerator {
	private const string GENERATE_COMPONENT_ACTION_ATTRIBUTE_QUALIFIED_NAME = "Jackey.Behaviours.Attributes.GenerateComponentActionAttribute";
	private const string BEHAVIOUR_OPERATION_ATTRIBUTE_QUALIFIED_NAME = "Jackey.Behaviours.Attributes.BehaviourOperationAttribute";
	private const string BEHAVIOUR_CONDITION_ATTRIBUTE_QUALIFIED_NAME = "Jackey.Behaviours.Attributes.BehaviourConditionAttribute";
	private const string COMPONENT_ACTION_INTERFACE_NAMESPACE = "Jackey.Behaviours.Actions";
	private const string COMPONENT_ACTION_INTERFACE_NAME = "IComponentAction";

	#region Templates

	private const string CLASS_TEMPLATE_HEAD = @"namespace Jackey.Behaviours.Generated {{
	public sealed partial class BehaviourMembers_Generated {{
";

	private const string CLASS_TEMPLATE_TAIL = "\t}}\n}}";

	private const string BASE_ACTION_TEMPLATE = CLASS_TEMPLATE_HEAD + @"
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Jackey.Behaviours.Attributes.DisplayName(""{0}"")]
		[Jackey.Behaviours.Attributes.SearchPath(""Generated/{0}"")]
		public sealed class {1} : Jackey.Behaviours.Actions.BehaviourAction<{2}> {{
			protected override Jackey.Behaviours.ExecutionStatus OnEnter() => ((Jackey.Behaviours.Actions.IComponentAction)GetTarget()).OnEnter(this);
			protected override Jackey.Behaviours.ExecutionStatus OnTick() => ((Jackey.Behaviours.Actions.IComponentAction)GetTarget()).OnTick(this);
			protected override void OnInterrupt() => ((Jackey.Behaviours.Actions.IComponentAction)GetTarget()).OnInterrupt(this);
			protected override void OnResult(Jackey.Behaviours.BehaviourResult result) => ((Jackey.Behaviours.Actions.IComponentAction)GetTarget()).OnResult(this, result);
			protected override void OnExit() => ((Jackey.Behaviours.Actions.IComponentAction)GetTarget()).OnExit(this);
		}}
" + CLASS_TEMPLATE_TAIL;

	private const string ARGS_ACTION_TEMPLATE = CLASS_TEMPLATE_HEAD + @"
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Jackey.Behaviours.Attributes.DisplayName(""{0}"")]
		[Jackey.Behaviours.Attributes.SearchPath(""Generated/{0}"")]
		public sealed class {1} : Jackey.Behaviours.Actions.BehaviourAction<{2}> {{
			[UnityEngine.SerializeField] private Jackey.Behaviours.Variables.BlackboardRef<{3}> m_args;

			protected override Jackey.Behaviours.ExecutionStatus OnEnter() => ((Jackey.Behaviours.Actions.IComponentAction<{3}>)GetTarget()).OnEnter(this, m_args.GetValue());
			protected override Jackey.Behaviours.ExecutionStatus OnTick() => ((Jackey.Behaviours.Actions.IComponentAction<{3}>)GetTarget()).OnTick(this, m_args.GetValue());
			protected override void OnInterrupt() => ((Jackey.Behaviours.Actions.IComponentAction<{3}>)GetTarget()).OnInterrupt(this, m_args.GetValue());
			protected override void OnResult(Jackey.Behaviours.BehaviourResult result) => ((Jackey.Behaviours.Actions.IComponentAction<{3}>)GetTarget()).OnResult(this, m_args.GetValue(), result);
			protected override void OnExit() => ((Jackey.Behaviours.Actions.IComponentAction<{3}>)GetTarget()).OnExit(this, m_args.GetValue());
		}}
" + CLASS_TEMPLATE_TAIL;

	private const string BASE_CONDITION_TEMPLATE = CLASS_TEMPLATE_HEAD + @"
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Jackey.Behaviours.Attributes.DisplayName(""{0}"")]
		[Jackey.Behaviours.Attributes.SearchPath(""Generated/{0}"")]
		public sealed class {1} : Jackey.Behaviours.Conditions.BehaviourCondition<{2}> {{ 
			public override bool Evaluate() => GetTarget().{3}();
		}}
" + CLASS_TEMPLATE_TAIL;

	private const string ARGS_CONDITION_TEMPLATE = CLASS_TEMPLATE_HEAD + @"
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Jackey.Behaviours.Attributes.DisplayName(""{0}"")]
		[Jackey.Behaviours.Attributes.SearchPath(""Generated/{0}"")]
		public sealed class {1} : Jackey.Behaviours.Conditions.BehaviourCondition<{2}> {{ 
			[UnityEngine.SerializeField] private Jackey.Behaviours.Variables.BlackboardRef<{3}> m_args;

			public override bool Evaluate() => GetTarget().{4}(m_args.GetValue());
		}}
" + CLASS_TEMPLATE_TAIL;

	private const string BASE_OPERATION_TEMPLATE = CLASS_TEMPLATE_HEAD + @"
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Jackey.Behaviours.Attributes.DisplayName(""{0}"")]
		[Jackey.Behaviours.Attributes.SearchPath(""Generated/{0}"")]
		public sealed class {1} : Jackey.Behaviours.Operations.Operation<{2}> {{
			protected override void OnExecute() {{
				GetTarget().{3}();
			}}
		}}
" + CLASS_TEMPLATE_TAIL;

	private const string ARGS_OPERATION_TEMPLATE = CLASS_TEMPLATE_HEAD + @"
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		[Jackey.Behaviours.Attributes.DisplayName(""{0}"")]
		[Jackey.Behaviours.Attributes.SearchPath(""Generated/{0}"")]
		public sealed class {1} : Jackey.Behaviours.Operations.Operation<{2}> {{
			[UnityEngine.SerializeField] private Jackey.Behaviours.Variables.BlackboardRef<{3}> m_args;
			
			protected override void OnExecute() {{
				GetTarget().{4}(m_args.GetValue());
			}}
		}}
" + CLASS_TEMPLATE_TAIL;

	#endregion

	#region Diagostics

#pragma warning disable RS2000 // RS2000: Enable analyzer release tracking for the analyzer project containing rule
	private static readonly DiagnosticDescriptor GENERATE_COMPONENT_ACTION_USAGE_ERROR_DIAGNOSTIC = new(
		"BEHAVIOUR001",
		"Class must implement IComponentAction to generate implementation",
		"A type attributed with GenerateComponentAction must implement IComponentAction or IComponentAction<T>",
		"usage",
		DiagnosticSeverity.Error, true);

	private static readonly DiagnosticDescriptor BEHAVIOUR_OPERATION_USAGE_ERROR_DIAGNOSTIC = new(
		"BEHAVIOUR002",
		"Behaviour operation method does not have the correct signature",
		"A method attributed with BehaviourOperationAttribute must be declared public, return void, and have at most one parameter",
		"usage",
		DiagnosticSeverity.Error, true);

	private static readonly DiagnosticDescriptor BEHAVIOUR_CONDITION_USAGE_ERROR_DIAGNOSTIC = new(
		"BEHAVIOUR003",
		"Behaviour condition method does not have the correct signature",
		"A method attributed with BehaviourConditionAttribute must be declared public, return bool, and have at most one parameter",
		"usage",
		DiagnosticSeverity.Error, true);
#pragma warning restore RS2000

	#endregion

	// Style: Namespace.Type.NestedType
	// NOTE: No '+' for nested types like in reflection.
	private static readonly SymbolDisplayFormat SIMPLE_QUALIFIED_TYPE_NAME_FORMAT = new(
		SymbolDisplayGlobalNamespaceStyle.Omitted,
		SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		SymbolDisplayGenericsOptions.None);

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		IncrementalValuesProvider<EquatableArray<GeneratorItem>> componentActions = context.SyntaxProvider.ForAttributeWithMetadataName(
			GENERATE_COMPONENT_ACTION_ATTRIBUTE_QUALIFIED_NAME,
			(node, ct) => node is TypeDeclarationSyntax,
			TransformComponentActions
		);

		IncrementalValuesProvider<EquatableArray<GeneratorItem>> operations = context.SyntaxProvider.ForAttributeWithMetadataName(
			BEHAVIOUR_OPERATION_ATTRIBUTE_QUALIFIED_NAME,
			(node, ct) => node is MethodDeclarationSyntax,
			TransformBehaviourOperation
		);

		IncrementalValuesProvider<EquatableArray<GeneratorItem>> conditions = context.SyntaxProvider.ForAttributeWithMetadataName(
			BEHAVIOUR_CONDITION_ATTRIBUTE_QUALIFIED_NAME,
			(node, ct) => node is MethodDeclarationSyntax,
			TransformBehaviourCondition
		);

		context.RegisterSourceOutput(componentActions, GenerateOutput);
		context.RegisterSourceOutput(operations, GenerateOutput);
		context.RegisterSourceOutput(conditions, GenerateOutput);
	}

	private static EquatableArray<GeneratorItem> TransformBehaviourOperation(
		GeneratorAttributeSyntaxContext context, CancellationToken ct) {
		IMethodSymbol methodSymbol = (IMethodSymbol)context.TargetSymbol;
		if (methodSymbol.DeclaredAccessibility != Accessibility.Public ||
		    methodSymbol.ReturnType.SpecialType != SpecialType.System_Void ||
		    methodSymbol.Parameters.Length > 1) {
			Diagnostic diagnostic = Diagnostic.Create(BEHAVIOUR_OPERATION_USAGE_ERROR_DIAGNOSTIC, context.TargetNode.GetLocation());
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(ItemKind.BehaviourOperation, "", "", "", "", "", diagnostic)]);
		}

		if (methodSymbol.Parameters.Length == 0) {
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(
					ItemKind.BehaviourOperation,
					methodSymbol.ContainingType.Name,
					methodSymbol.ContainingType.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					"",
					"",
					methodSymbol.Name,
					null)]);
		}
		else {
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(
					ItemKind.BehaviourOperationArg,
					methodSymbol.ContainingType.Name,
					methodSymbol.ContainingType.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					methodSymbol.Parameters[0].Type.Name,
					methodSymbol.Parameters[0].Type.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					methodSymbol.Name,
					null)]);
		}
	}

	private static EquatableArray<GeneratorItem> TransformBehaviourCondition(
		GeneratorAttributeSyntaxContext context, CancellationToken ct) {
		IMethodSymbol methodSymbol = (IMethodSymbol)context.TargetSymbol;
		if (methodSymbol.DeclaredAccessibility != Accessibility.Public ||
		    methodSymbol.ReturnType.SpecialType != SpecialType.System_Boolean ||
		    methodSymbol.Parameters.Length > 1) {
			Diagnostic diagnostic = Diagnostic.Create(BEHAVIOUR_CONDITION_USAGE_ERROR_DIAGNOSTIC, context.TargetNode.GetLocation());
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(ItemKind.BehaviourOperation, "", "", "", "", "", diagnostic)]);
		}

		if (methodSymbol.Parameters.Length == 0) {
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(
					ItemKind.BehaviourCondition,
					methodSymbol.ContainingType.Name,
					methodSymbol.ContainingType.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					"",
					"",
					methodSymbol.Name,
					null)]);
		}
		else {
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(
					ItemKind.BehaviourConditionArg,
					methodSymbol.ContainingType.Name,
					methodSymbol.ContainingType.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					methodSymbol.Parameters[0].Type.Name,
					methodSymbol.Parameters[0].Type.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					methodSymbol.Name,
					null)]);
		}
	}

	private static EquatableArray<GeneratorItem> TransformComponentActions(
		GeneratorAttributeSyntaxContext context, CancellationToken ct) {
		ITypeSymbol typeSymbol = (ITypeSymbol)context.TargetSymbol;

		List<INamedTypeSymbol> interfaceTypes = new();
		if (!FindComponentInterfaces(typeSymbol.Interfaces, interfaceTypes)) {
			Diagnostic diagnostic = Diagnostic.Create(GENERATE_COMPONENT_ACTION_USAGE_ERROR_DIAGNOSTIC, context.TargetNode.GetLocation());
			return new EquatableArray<GeneratorItem>([
				new GeneratorItem(ItemKind.ComponentAction, "", "", "", "", "", diagnostic)]);
		}

		GeneratorItem[] items = new GeneratorItem[interfaceTypes.Count];
		for (int i = 0; i < interfaceTypes.Count; i++) {
			if (!interfaceTypes[i].IsGenericType) {
				items[i] = new GeneratorItem(
					ItemKind.ComponentAction,
					typeSymbol.Name,
					typeSymbol.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					"",
					"",
					"",
					null);
			}
			else {
				items[i] = new GeneratorItem(
					ItemKind.ComponentActionArg,
					typeSymbol.Name,
					typeSymbol.ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					interfaceTypes[i].TypeArguments[0].Name,
					interfaceTypes[i].TypeArguments[0].ToDisplayString(SIMPLE_QUALIFIED_TYPE_NAME_FORMAT),
					"",
					null);
			}
		}

		return items;

		static bool FindComponentInterfaces(ImmutableArray<INamedTypeSymbol> types, List<INamedTypeSymbol> outInterfaceTypes) {
			foreach (INamedTypeSymbol typeSymbol in types) {
				if (typeSymbol.ContainingNamespace.ToString() != COMPONENT_ACTION_INTERFACE_NAMESPACE)
					continue;
				if (typeSymbol.ContainingType != null)
					continue;

				if (typeSymbol.Name == COMPONENT_ACTION_INTERFACE_NAME &&
				    (!typeSymbol.IsGenericType || typeSymbol.TypeArguments.Length == 1)) {
					outInterfaceTypes.Add(typeSymbol);
				}
			}

			return outInterfaceTypes.Count > 0;
		}
	}

	private static void GenerateOutput(SourceProductionContext context, EquatableArray<GeneratorItem> items) {
		foreach (GeneratorItem item in items.Value) {
			GenerateOutput(context, item);
		}
	}

	private static void GenerateOutput(SourceProductionContext context, GeneratorItem item) {
		if (item.Diagnostic != null) {
			context.ReportDiagnostic(item.Diagnostic);
			return;
		}

		switch (item.Kind) {
			case ItemKind.ComponentAction: {
				string className = item.QualifiedTargetType.Replace('.', '_');
				string source = string.Format(
					BASE_ACTION_TEMPLATE,
					item.TargetType,
					className + "_Generated",
					item.QualifiedTargetType);
				context.AddSource(className + ".g.cs", source);
				break;
			}
			case ItemKind.ComponentActionArg: {
				string className = $"{item.QualifiedTargetType.Replace('.', '_')}_{item.QualifiedArgType.Replace('.', '_')}";
				string source = string.Format(
					ARGS_ACTION_TEMPLATE,
					$"{item.TargetType}<{item.ArgType}>",
					className + "_Generated",
					item.QualifiedTargetType,
					item.QualifiedArgType);
				context.AddSource(className + ".g.cs", source);
				break;
			}
			case ItemKind.BehaviourCondition: {
				string className = $"{item.QualifiedTargetType.Replace('.', '_')}_{item.MethodName}";
				string source = string.Format(
					BASE_CONDITION_TEMPLATE,
					$"{item.TargetType}.{item.MethodName}()",
					className + "_Generated",
					item.QualifiedTargetType,
					item.MethodName);
				context.AddSource(className + ".g.cs", source);
				break;
			}
			case ItemKind.BehaviourConditionArg: {
				string className = $"{item.QualifiedTargetType.Replace('.', '_')}_{item.MethodName}_{item.ArgType}";
				string source = string.Format(
					ARGS_CONDITION_TEMPLATE,
					$"{item.TargetType}.{item.MethodName}({item.ArgType})",
					className + "_Generated",
					item.QualifiedTargetType,
					item.QualifiedArgType,
					item.MethodName);
				context.AddSource(className + ".g.cs", source);
				break;
			}
			case ItemKind.BehaviourOperation: {
				string className = $"{item.QualifiedTargetType.Replace('.', '_')}_{item.MethodName}";
				string source = string.Format(
					BASE_OPERATION_TEMPLATE,
					$"{item.TargetType}.{item.MethodName}()",
					className + "_Generated",
					item.QualifiedTargetType,
					item.MethodName);
				context.AddSource(className + ".g.cs", source);
				break;
			}
			case ItemKind.BehaviourOperationArg: {
				string className = $"{item.QualifiedTargetType.Replace('.', '_')}_{item.MethodName}_{item.ArgType}";
				string source = string.Format(
					ARGS_OPERATION_TEMPLATE,
					$"{item.TargetType}.{item.MethodName}({item.ArgType})",
					className + "_Generated",
					item.QualifiedTargetType,
					item.QualifiedArgType,
					item.MethodName);
				context.AddSource(className + ".g.cs", source);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
	}


	private enum ItemKind {
		ComponentAction,
		ComponentActionArg,
		BehaviourCondition,
		BehaviourConditionArg,
		BehaviourOperation,
		BehaviourOperationArg,
	}

	// The same data structure is used for all items for simplicity. 'Kind' specifies the type of item, and should be
	// used to infer which fields will be present. If 'Diagnostic' is set, all other data should be ignored.
	//
	// The choice of using a record here is very intentional, the incremental generator needs the data model to be
	// deterministically IEquatable in order to properly detect changes. The record construct implements IEquatable for
	// us and the data we store is only basic comparable types.
	private record struct GeneratorItem(
		ItemKind Kind,
		string TargetType,
		string QualifiedTargetType,
		string ArgType,
		string QualifiedArgType,
		string MethodName,
		Diagnostic? Diagnostic);

	private readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>> where T : IEquatable<T> {
		public readonly T[] Value = array;

		public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);
		public bool Equals(EquatableArray<T> other) => Value.AsSpan().SequenceEqual(other.Value);

		public override int GetHashCode() {
			int hash = Value.Length;
			foreach (T item in Value) {
				hash = unchecked(hash * 31 + item.GetHashCode());
			}

			return hash;
		}

		public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);
		public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
		public static implicit operator EquatableArray<T>(T[] array) => new(array);
	}
}
