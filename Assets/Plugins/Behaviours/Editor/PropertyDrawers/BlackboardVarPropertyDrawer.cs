using System;
using System.Reflection;
using Jackey.Behaviours.Core.Blackboard;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(BlackboardVar))]
	public class BlackboardVarPropertyDrawer : PropertyDrawer {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement root = new() {
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					alignItems = Align.Center,
				},
			};

			SerializedProperty nameProperty = property.FindPropertyRelative("m_variableName");
			PropertyField nameField = new PropertyField(nameProperty) {
				label = string.Empty,
				style = { width = Length.Percent(50f) },
			};
			root.Add(nameField);

			VisualElement valueField = CreateValueField(property);
			valueField.style.width = Length.Percent(50f);
			root.Add(valueField);

			return root;
		}

		private VisualElement CreateValueField(SerializedProperty property) {
			SerializedProperty typeNameProperty = property.FindPropertyRelative("m_serializedTypeName");
			Type valueType = Type.GetType(typeNameProperty.stringValue);

			// Unity Object
			if (typeof(UnityEngine.Object).IsAssignableFrom(valueType)) {
				SerializedProperty unityObjectProperty = property.FindPropertyRelative("m_unityObjectValue");

				ObjectField field = new ObjectField() {
					objectType = valueType,
					value = unityObjectProperty.objectReferenceValue,
					allowSceneObjects = true,
				};
				field.BindProperty(unityObjectProperty);

				return field;
			}

			SerializedProperty primitiveValueProperty = property.FindPropertyRelative("m_primitiveValue");

			// Primitives
			if (valueType == typeof(bool))
				return PrimitiveField<Toggle, bool>(primitiveValueProperty);

			if (valueType == typeof(string))
				return PrimitiveField<TextField, string>(primitiveValueProperty);

			if (valueType == typeof(int))
				return PrimitiveField<IntegerField, int>(primitiveValueProperty);

			if (valueType == typeof(uint))
				return PrimitiveField<UnsignedIntegerField, uint>(primitiveValueProperty);

			if (valueType == typeof(long))
				return PrimitiveField<LongField, long>(primitiveValueProperty);

			if (valueType == typeof(ulong))
				return PrimitiveField<UnsignedLongField, ulong>(primitiveValueProperty);

			if (valueType == typeof(float))
				return PrimitiveField<FloatField, float>(primitiveValueProperty);

			if (valueType == typeof(double))
				return PrimitiveField<DoubleField, double>(primitiveValueProperty);

			if (typeof(Enum).IsAssignableFrom(valueType)) {
				if (valueType.GetCustomAttribute(typeof(FlagsAttribute)) != null)
					return EnumFlagsField(valueType, primitiveValueProperty);

				return EnumField(valueType, primitiveValueProperty);
			}

			SerializedProperty boxedValueProperty = property.FindPropertyRelative("m_boxedValue");

			// Vectors
			if (valueType == typeof(Vector2))
				return ManagedField<Vector2Field, Vector2>(boxedValueProperty);

			if (valueType == typeof(Vector2Int))
				return ManagedField<Vector2IntField, Vector2Int>(boxedValueProperty);

			if (valueType == typeof(Vector3))
				return ManagedField<Vector3Field, Vector3>(boxedValueProperty);

			if (valueType == typeof(Vector3Int))
				return ManagedField<Vector3IntField, Vector3Int>(boxedValueProperty);

			if (valueType == typeof(Vector4))
				return ManagedField<Vector4Field, Vector4>(boxedValueProperty);

			// Geometry
			if (valueType == typeof(Rect))
				return ManagedField<RectField, Rect>(boxedValueProperty);

			if (valueType == typeof(RectInt))
				return ManagedField<RectIntField, RectInt>(boxedValueProperty);

			if (valueType == typeof(Bounds))
				return ManagedField<BoundsField, Bounds>(boxedValueProperty);

			if (valueType == typeof(BoundsInt))
				return ManagedField<BoundsIntField, BoundsInt>(boxedValueProperty);

			// Others
			if (valueType == typeof(Color))
				return ManagedField<ColorField, Color>(boxedValueProperty);

			if (valueType == typeof(Gradient))
				return ManagedField<GradientField, Gradient>(boxedValueProperty);

			if (valueType == typeof(LayerMask))
				return ManagedField<LayerMaskField, int>(boxedValueProperty);

			if (valueType == typeof(AnimationCurve))
				return ManagedField<CurveField, AnimationCurve>(boxedValueProperty);

			if (valueType == typeof(Hash128))
				return ManagedField<Hash128Field, Hash128>(boxedValueProperty);

			return new Label(valueType?.Name ?? "<color=red>Unknown Type</color>") { name = "UserType" };
		}

		private VisualElement PrimitiveField<TField, TType>(SerializedProperty valueProperty) where TField : BaseField<TType>, new() {
			TField field = new TField() { value = !string.IsNullOrEmpty(valueProperty.stringValue) ? (TType)Convert.ChangeType(valueProperty.stringValue, typeof(TType)) : default };
			TrackPrimitiveValueChanges(field, valueProperty);

			return field;
		}

		private VisualElement EnumField(Type enumType, SerializedProperty valueProperty) {
			EnumField field = new EnumField((Enum)Enum.ToObject(enumType, 0));

			string propertyValue = valueProperty.stringValue;
			if (string.IsNullOrEmpty(propertyValue))
				field.value = default;
			else
				field.value = (Enum)Enum.Parse(enumType, propertyValue);

			TrackEnumValueChanges(field, valueProperty);

			return field;
		}

		private VisualElement ManagedField<TField, TType>(SerializedProperty valueProperty) where TField : BaseField<TType>, new() {
			TField field = new TField() { value = (TType)(valueProperty.managedReferenceValue ?? default(TType)) };
			TrackValueChanges(field, valueProperty);

			return field;
		}

		private VisualElement EnumFlagsField(Type enumType, SerializedProperty valueProperty) {
			EnumFlagsField field = new EnumFlagsField((Enum)Enum.ToObject(enumType, 0));

			string propertyValue = valueProperty.stringValue;
			if (string.IsNullOrEmpty(propertyValue))
				field.value = default;
			else
				field.value = (Enum)Enum.Parse(enumType, propertyValue);

			TrackEnumValueChanges(field, valueProperty);

			return field;
		}

		private void TrackPrimitiveValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty) {
			field.RegisterValueChangedCallback(evt => {
				valueProperty.serializedObject.Update();
				valueProperty.stringValue = evt.newValue.ToString();
				valueProperty.serializedObject.ApplyModifiedProperties();

				EditorUtility.SetDirty(valueProperty.serializedObject.targetObject);
			});
			// field.TrackPropertyValue(valueProperty, _ => field.value = (TType)(valueProperty.managedReferenceValue ?? default(TType)));
		}

		private void TrackEnumValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty) where T : Enum {
			field.RegisterValueChangedCallback(evt => {
				valueProperty.serializedObject.Update();
				valueProperty.stringValue = Convert.ToInt32(evt.newValue).ToString();
				valueProperty.serializedObject.ApplyModifiedProperties();

				EditorUtility.SetDirty(valueProperty.serializedObject.targetObject);
			});
			// field.TrackPropertyValue(valueProperty, _ => field.value = (TType)(valueProperty.managedReferenceValue ?? default(TType)));
		}

		private void TrackValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty) {
			field.RegisterValueChangedCallback(evt => {
				valueProperty.serializedObject.Update();
				valueProperty.managedReferenceValue = evt.newValue;
				valueProperty.serializedObject.ApplyModifiedProperties();

				EditorUtility.SetDirty(valueProperty.serializedObject.targetObject);
			});
			// field.TrackPropertyValue(valueProperty, _ => field.value = (TType)(valueProperty.managedReferenceValue ?? default(TType)));
		}
	}
}
