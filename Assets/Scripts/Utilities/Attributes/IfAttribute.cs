using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Jackey.Utilities.Attributes.PropertyDrawers;
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Base attribute for conditional drawing of serialized properties.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public class IfAttribute : PropertyAttribute {
		public string Other { get; }
		internal Comparison Method { get; }
		internal ValueType OtherType { get; }

		public bool BoolValue { get; }
		public double NumberValue { get; }

		private IfAttribute(string other, Comparison comparison) {
			Other = other;
			Method = comparison;
		}

		protected IfAttribute(string other, BoolComparison comparison, bool value) : this(other, (Comparison)comparison) {
			OtherType = ValueType.Boolean;
			BoolValue = value;
		}

		protected IfAttribute(string other, NumberComparison comparison, double value) : this(other, (Comparison)comparison) {
			OtherType = ValueType.Number;
			NumberValue = value;
		}

		protected IfAttribute(string other, ObjectComparison comparison, Object _) : this(other, (Comparison)comparison) {
			OtherType = ValueType.Object;
		}

#if UNITY_EDITOR
		public bool EvaluateCondition(SerializedProperty property) {
			return IfAttributeDrawer.EvaluateCondition(property, this);
		}

		[CanBeNull]
		public SerializedProperty FindOtherProperty(SerializedProperty property) {
			return IfAttributeDrawer.FindOtherProperty(property, this);
		}
#endif

		public enum BoolComparison {
			Equal = Comparison.Equal,
			NotEqual = Comparison.NotEqual,
		}

		public enum NumberComparison {
			Equal = Comparison.Equal,
			NotEqual = Comparison.NotEqual,
			Greater = Comparison.Greater,
			GreaterOrEqual = Comparison.GreaterOrEqual,
			Less = Comparison.Less,
			LessOrEqual = Comparison.LessOrEqual,
			AND = Comparison.AND,
			NOT = Comparison.NOT,
			OR = Comparison.OR,
		}

		public enum ObjectComparison {
			Null = Comparison.Null,
			NotNull = Comparison.NotNull,
		}

		internal enum Comparison {
			Equal,
			NotEqual,
			Greater,
			GreaterOrEqual,
			Less,
			LessOrEqual,
			AND,
			NOT,
			OR,
			Null,
			NotNull,
		}

		internal enum ValueType {
			Boolean,
			Number,
			Object,
		}
	}

	/// <summary>
	/// Show this field in the inspector only when a comparison with another field is true
	/// </summary>
	public class ShowIfAttribute : IfAttribute {
		public ShowIfAttribute(string other, BoolComparison comparison, bool value) : base(other, comparison, value) { }
		public ShowIfAttribute(string other, NumberComparison comparison, double value) : base(other, comparison, value) { }
		public ShowIfAttribute(string other, ObjectComparison comparison) : base(other, comparison, null) { }

#if UNITY_EDITOR
		public void Bind(VisualElement element, SerializedProperty property) {
			element.style.display = EvaluateCondition(property) ? DisplayStyle.Flex : DisplayStyle.None;

			element.TrackPropertyValue(
				FindOtherProperty(property),
				_ => element.style.display = EvaluateCondition(property) ? DisplayStyle.Flex : DisplayStyle.None
			);
		}
#endif
	}

	/// <summary>
	/// Same as <see cref="ShowIfAttribute"/> but is meant to be used on fields that have another property drawer.
	/// That drawer can then easily get the result of the condition by either using <see cref="ShowIfAttribute.Bind"/> for VisualElements or
	/// <see cref="IfAttribute.EvaluateCondition(SerializedProperty)"/> for IMGUI
	/// </summary>
	public sealed class CustomShowIfAttribute : ShowIfAttribute {
		public CustomShowIfAttribute(string other, BoolComparison comparison, bool value) : base(other, comparison, value) { }
		public CustomShowIfAttribute(string other, NumberComparison comparison, double value) : base(other, comparison, value) { }
		public CustomShowIfAttribute(string other, ObjectComparison comparison) : base(other, comparison) { }
	}

	/// <summary>
	/// Have this field enabled for editing in the inspector only when a comparison with another field is true
	/// </summary>
	public class EnableIfAttribute : IfAttribute {
		public EnableIfAttribute(string other, BoolComparison comparison, bool value) : base(other, comparison, value) { }
		public EnableIfAttribute(string other, NumberComparison comparison, double value) : base(other, comparison, value) { }
		public EnableIfAttribute(string other, ObjectComparison comparison) : base(other, comparison, null) { }

#if UNITY_EDITOR
		public void Bind(VisualElement element, SerializedProperty property) {
			element.SetEnabled(EvaluateCondition(property));

			element.TrackPropertyValue(
				FindOtherProperty(property),
				_ => element.SetEnabled(EvaluateCondition(property))
			);
		}
#endif
	}

	/// <summary>
	/// Same as <see cref="EnableIfAttribute"/> but is meant to be used on fields that have another property drawer.
	/// That drawer can then easily get the result of the condition by either using <see cref="EnableIfAttribute.Bind"/> for VisualElements or
	/// <see cref="IfAttribute.EvaluateCondition(SerializedProperty)"/> for IMGUI
	/// </summary>
	public sealed class CustomEnableIfAttribute : EnableIfAttribute {
		public CustomEnableIfAttribute(string other, BoolComparison comparison, bool value) : base(other, comparison, value) { }
		public CustomEnableIfAttribute(string other, NumberComparison comparison, double value) : base(other, comparison, value) { }
		public CustomEnableIfAttribute(string other, ObjectComparison comparison) : base(other, comparison) { }
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		public abstract class IfAttributeDrawer : PropertyDrawer {
			public static bool EvaluateCondition(SerializedProperty property, IfAttribute attr) {
				SerializedProperty otherProperty = FindOtherProperty(property, attr);

				if (!PropertyMatchesCheck(otherProperty, attr.OtherType))
					return false;

				return EvaluateComparison(otherProperty, attr);
			}

			[CanBeNull]
			public static SerializedProperty FindOtherProperty(SerializedProperty property, IfAttribute attr) {
				string propertyPath = property.propertyPath;
				int lastDepthIndex = propertyPath.LastIndexOf('.');

				string currentDepthPath = string.Empty;
				if (lastDepthIndex != -1)
					currentDepthPath = $"{propertyPath[..lastDepthIndex]}.";

				string otherPath = $"{currentDepthPath}{attr.Other}";
				return property.serializedObject.FindProperty(otherPath);
			}

			private static bool PropertyMatchesCheck(SerializedProperty property, IfAttribute.ValueType valueType) {
				if (property == null)
					return false;

				return valueType switch {
					IfAttribute.ValueType.Boolean => property.propertyType is SerializedPropertyType.Boolean,
					IfAttribute.ValueType.Number => property.propertyType is SerializedPropertyType.Float or SerializedPropertyType.Integer or SerializedPropertyType.Enum or SerializedPropertyType.LayerMask,
					IfAttribute.ValueType.Object => property.propertyType is SerializedPropertyType.ObjectReference,
					_ => throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null),
				};
			}

			private static bool EvaluateComparison(SerializedProperty property, IfAttribute attr) {
				IfAttribute.Comparison comparison = attr.Method;

				switch (attr.OtherType) {
					case IfAttribute.ValueType.Boolean:
						if (comparison is IfAttribute.Comparison.Equal)
							return property.boolValue == attr.BoolValue;

						return property.boolValue != attr.BoolValue;
					case IfAttribute.ValueType.Number:
						double numberValue = GetNumberValue(property);
						double otherValue = attr.NumberValue;

						return attr.Method switch {
							IfAttribute.Comparison.Equal => numberValue == otherValue,
							IfAttribute.Comparison.NotEqual => numberValue != otherValue,
							IfAttribute.Comparison.Greater => numberValue > otherValue,
							IfAttribute.Comparison.GreaterOrEqual => numberValue >= otherValue,
							IfAttribute.Comparison.LessOrEqual => numberValue <= otherValue,
							IfAttribute.Comparison.Less => numberValue < otherValue,
							IfAttribute.Comparison.AND => ((long)numberValue & (long)otherValue) == (long)otherValue,
							IfAttribute.Comparison.NOT => ((long)numberValue & (long)otherValue) == 0L,
							IfAttribute.Comparison.OR => ((long)numberValue & (long)otherValue) != 0L,
							_ => throw new ArgumentOutOfRangeException(),
						};
					case IfAttribute.ValueType.Object:
						if (comparison is IfAttribute.Comparison.Null)
							return property.objectReferenceValue == null;

						return property.objectReferenceValue != null;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			private static double GetNumberValue(SerializedProperty property) {
				if (property.propertyType is SerializedPropertyType.Integer) {
					int intValue = property.intValue;
					if (intValue != 0)
						return intValue;

					uint uIntValue = property.uintValue;
					if (uIntValue != 0)
						return uIntValue;

					long longValue = property.longValue;
					if (longValue != 0)
						return longValue;

					return property.ulongValue;
				}

				if (property.propertyType is SerializedPropertyType.Float) {
					float floatValue = property.floatValue;
					if (floatValue != 0f)
						return floatValue;

					return property.doubleValue;
				}

				if (property.propertyType is SerializedPropertyType.Enum) {
					return property.enumValueFlag;
				}

				if (property.propertyType is SerializedPropertyType.LayerMask) {
					return property.intValue;
				}

				return 0;
			}
		}

		[CustomPropertyDrawer(typeof(ShowIfAttribute), false)]
		public sealed class ShowIfAttributeDrawer : IfAttributeDrawer {
			public override VisualElement CreatePropertyGUI(SerializedProperty property) {
				ShowIfAttribute attr = (ShowIfAttribute)attribute;

				PropertyField field = new PropertyField(property);
				attr.Bind(field, property);

				return field;
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				if (EvaluateCondition(property, (IfAttribute)attribute))
					return EditorGUI.GetPropertyHeight(property, label);

				return 0f;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (!EvaluateCondition(property, (IfAttribute)attribute))
					return;

				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.PropertyField(position, property, label);
				EditorGUI.EndProperty();
			}
		}

		[CustomPropertyDrawer(typeof(EnableIfAttribute), false)]
		public sealed class EnableIfAttributeDrawer : IfAttributeDrawer {
			public override VisualElement CreatePropertyGUI(SerializedProperty property) {
				EnableIfAttribute attr = (EnableIfAttribute)attribute;

				PropertyField field = new PropertyField(property);
				attr.Bind(field, property);

				return field;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.BeginDisabledGroup(!EvaluateCondition(property, (IfAttribute)attribute));

				EditorGUI.PropertyField(position, property, label);

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndProperty();
			}
		}
	}
#endif
}
