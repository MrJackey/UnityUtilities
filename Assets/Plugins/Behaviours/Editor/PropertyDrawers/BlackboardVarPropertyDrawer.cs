using System;
using System.Reflection;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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

				if (Application.IsPlaying(property.serializedObject.targetObject))
					SetupRuntimeField(field, unityObjectProperty);
				else
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
				return JsonField<Vector2IntField, Vector2Int>(primitiveValueProperty);

			if (valueType == typeof(Vector3))
				return ManagedField<Vector3Field, Vector3>(boxedValueProperty);

			if (valueType == typeof(Vector3Int))
				return JsonField<Vector3IntField, Vector3Int>(primitiveValueProperty);

			if (valueType == typeof(Vector4))
				return ManagedField<Vector4Field, Vector4>(boxedValueProperty);

			// Geometry
			if (valueType == typeof(Rect))
				return JsonField<RectField, Rect>(primitiveValueProperty);

			if (valueType == typeof(RectInt))
				return JsonField<RectIntField, RectInt>(primitiveValueProperty);

			if (valueType == typeof(Bounds))
				return JsonField<BoundsField, Bounds>(primitiveValueProperty);

			if (valueType == typeof(BoundsInt))
				return JsonField<BoundsIntField, BoundsInt>(primitiveValueProperty);

			// Others
			if (valueType == typeof(Color))
				return JsonField<ColorField, Color>(primitiveValueProperty);

			if (valueType == typeof(Gradient)) {
				GradientField field = JsonField<GradientField, Gradient>(primitiveValueProperty);
				field.TrackPropertyValue(primitiveValueProperty, valueProperty => {
					// For some reason the gradient field does not clear its background on null values... So I'll do it myself
					if (string.IsNullOrEmpty(valueProperty.stringValue))
						field.Q<VisualElement>(null, "unity-gradient-field__content").style.backgroundImage = null;
				});

				return field;
			}

			if (valueType == typeof(LayerMask))
				return JsonField<LayerMaskField, int>(primitiveValueProperty);

			if (valueType == typeof(AnimationCurve))
				return JsonField<CurveField, AnimationCurve>(primitiveValueProperty);

			if (valueType == typeof(Hash128))
				return JsonField<Hash128Field, Hash128>(primitiveValueProperty);

			if (valueType != null)
				return new Label(valueType.Name) { name = "UserType" };

			return new Button(() => HandleUnknownType(property)) { name = "UnknownType", text = "Unknown Type..." };
		}

		private TField PrimitiveField<TField, TType>(SerializedProperty valueProperty) where TField : BaseField<TType>, new() {
			TField field = new TField() { value = !string.IsNullOrEmpty(valueProperty.stringValue) ? (TType)Convert.ChangeType(valueProperty.stringValue, typeof(TType)) : default };
			TrackPrimitiveValueChanges(field, valueProperty);

			return field;
		}

		private EnumField EnumField(Type enumType, SerializedProperty valueProperty) {
			EnumField field = new EnumField((Enum)Enum.ToObject(enumType, 0));

			string propertyValue = valueProperty.stringValue;
			if (string.IsNullOrEmpty(propertyValue))
				field.value = default;
			else
				field.value = (Enum)Enum.Parse(enumType, propertyValue);

			TrackEnumValueChanges(field, valueProperty, enumType);

			return field;
		}

		private EnumFlagsField EnumFlagsField(Type enumType, SerializedProperty valueProperty) {
			EnumFlagsField field = new EnumFlagsField((Enum)Enum.ToObject(enumType, 0));

			string propertyValue = valueProperty.stringValue;
			if (string.IsNullOrEmpty(propertyValue))
				field.value = default;
			else
				field.value = (Enum)Enum.Parse(enumType, propertyValue);

			TrackEnumValueChanges(field, valueProperty, enumType);

			return field;
		}

		private TField ManagedField<TField, TType>(SerializedProperty valueProperty) where TField : BaseField<TType>, new() {
			TField field = new TField() { value = (TType)(valueProperty.managedReferenceValue ?? default(TType)) };
			TrackManagedValueChanges(field, valueProperty);

			return field;
		}

		private TField JsonField<TField, TType>(SerializedProperty valueProperty) where TField : BaseField<TType>, new() {
			object propertyValue = JsonUtility.FromJson(valueProperty.stringValue, typeof(JsonWrapper<TType>));
			TField field = new TField() { value = propertyValue != null ? ((JsonWrapper<TType>)propertyValue).Value : default };
			TrackJsonValueChanges(field, valueProperty);

			return field;
		}

		private void TrackPrimitiveValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty) {
			if (Application.IsPlaying(valueProperty.serializedObject.targetObject)) { // Runtime
				SetupRuntimeField(field, valueProperty);
			}
			else { // Edit
				field.RegisterValueChangedCallback(evt => {
					SerializedObject serializedObject = valueProperty.serializedObject;

					serializedObject.Update();
					valueProperty.stringValue = evt.newValue.ToString();
					serializedObject.ApplyModifiedProperties();

					EditorUtility.SetDirty(serializedObject.targetObject);
				});

				// Undo
				field.TrackPropertyValue(valueProperty, property => {
					// If the blackboard removes this drawer due to property change, do nothing
					if (field.panel == null) return;

					string value = property.stringValue;
					field.SetValueWithoutNotify(!string.IsNullOrEmpty(value) ? (T)Convert.ChangeType(value, typeof(T)) : default);
				});
			}
		}

		private void TrackEnumValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty, Type enumType) where T : Enum {
			if (Application.IsPlaying(valueProperty.serializedObject.targetObject)) { // Runtime
				SetupRuntimeField(field, valueProperty);
			}
			else { // Edit
				field.RegisterValueChangedCallback(evt => {
					SerializedObject serializedObject = valueProperty.serializedObject;

					serializedObject.Update();
					valueProperty.stringValue = Convert.ToInt32(evt.newValue).ToString();
					serializedObject.ApplyModifiedProperties();

					EditorUtility.SetDirty(serializedObject.targetObject);
				});

				// Undo
				field.TrackPropertyValue(valueProperty, property => {
					// If the blackboard removes this drawer due to property change, do nothing
					if (field.panel == null) return;

					string propertyValue = property.stringValue;

					if (string.IsNullOrEmpty(propertyValue)) {
						field.SetValueWithoutNotify(default);
					}
					else {
						switch (field) {
							case EnumField enumField:
								enumField.SetValueWithoutNotify((Enum)Enum.Parse(enumType, propertyValue));
								break;
							case EnumFlagsField enumFlagsField:
								enumFlagsField.SetValueWithoutNotify((Enum)Enum.Parse(enumType, propertyValue));
								break;
						}
					}
				});
			}
		}

		private void TrackManagedValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty) {
			if (Application.IsPlaying(valueProperty.serializedObject.targetObject)) { // Runtime
				SetupRuntimeField(field, valueProperty);
			}
			else { // Edit
				field.RegisterValueChangedCallback(evt => {
					SerializedObject serializedObject = valueProperty.serializedObject;

					serializedObject.Update();
					valueProperty.managedReferenceValue = evt.newValue;
					serializedObject.ApplyModifiedProperties();

					EditorUtility.SetDirty(serializedObject.targetObject);
				});

				// Undo
				field.TrackPropertyValue(valueProperty, property => {
					// If the blackboard removes this drawer due to property change, do nothing
					if (field.panel == null) return;

					field.SetValueWithoutNotify((T)(property.managedReferenceValue ?? default(T)));
				});
			}
		}

		private void TrackJsonValueChanges<T>(BaseField<T> field, SerializedProperty valueProperty) {
			if (Application.IsPlaying(valueProperty.serializedObject.targetObject)) { // Runtime
				SetupRuntimeField(field, valueProperty);
			}
			else { // Edit
				field.RegisterValueChangedCallback(evt => {
					SerializedObject serializedObject = valueProperty.serializedObject;

					serializedObject.Update();
					valueProperty.stringValue = JsonUtility.ToJson(new JsonWrapper<T>(evt.newValue));
					serializedObject.ApplyModifiedProperties();

					EditorUtility.SetDirty(serializedObject.targetObject);
				});

				// Undo
				field.TrackPropertyValue(valueProperty, property => {
					// If the blackboard removes this drawer due to property change, do nothing
					if (field.panel == null) return;

					object propertyValue = JsonUtility.FromJson(property.stringValue, typeof(JsonWrapper<T>));
					field.SetValueWithoutNotify(propertyValue != null ? ((JsonWrapper<T>)propertyValue).Value : default);
				});
			}
		}

		private void SetupRuntimeField<T>(BaseField<T> field, SerializedProperty property) {
			BlackboardVar variable = GetBlackboardVariable(property);

			if (variable == null)
				return;

			field.schedule.Execute(() => {
				field.value = variable.GetValue<T>();
			}).Every(1/60L);

			field.RegisterValueChangedCallback(evt => {
				variable.SetValue(evt.newValue);
			});
		}

		private void HandleUnknownType(SerializedProperty property) {
			string fromTypeName = property.FindPropertyRelative("m_serializedTypeName").stringValue;

			int namespaceClassLength = fromTypeName.IndexOf(',');
			int namespaceLength = fromTypeName.LastIndexOf('.', namespaceClassLength, namespaceClassLength);
			int assemblyEnd = fromTypeName.IndexOf(',', namespaceClassLength + 1);

			string assembly = fromTypeName[(namespaceClassLength + 2)..assemblyEnd];
			string ns = string.Empty;
			string typeName;

			if (namespaceLength != -1) {
				ns = fromTypeName[0..namespaceLength];
				typeName = fromTypeName[(namespaceLength + 1)..namespaceClassLength];
			}
			else {
				typeName = fromTypeName[0..namespaceClassLength];
			}

			int option = EditorUtility.DisplayDialogComplex(
				"Unknown Blackboard Variable Type",
				$"Assembly: {assembly}\nNamespace: {ns}\nType: {typeName}",
				"Fix",
				"Cancel",
				"Delete"
			);

			switch (option) {
				case 0: // Fix
					TypeProvider.Instance.AskForType(EditorGUIUtility.GetMainWindowPosition().center, BlackboardPropertyDrawer.s_blackboardSearchTypes, type => {
						Debug.Assert(BlackboardPropertyDrawer.s_lastFocusedDrawer != null);
						BlackboardPropertyDrawer.s_lastFocusedDrawer.ChangeVariableType(GetVariableIndex(property), type);
					});
					break;
				case 1: // Cancel
					return;
				case 2: // Delete
					Debug.Assert(BlackboardPropertyDrawer.s_lastFocusedDrawer != null);
					BlackboardPropertyDrawer.s_lastFocusedDrawer.DeleteVariable(GetVariableIndex(property));
					break;
			}
		}

		private BlackboardVar GetBlackboardVariable(SerializedProperty property) {
			Object targetObject = property.serializedObject.targetObject;

			Blackboard blackboard = null;
			if (targetObject is ObjectBehaviour behaviour)
				blackboard = behaviour.Blackboard;
			else if (targetObject is BehaviourOwner owner)
				blackboard = owner.Blackboard;

			if (blackboard == null)
				return null;

			int variableIndex = GetVariableIndex(property);

			return blackboard.m_variables[variableIndex];
		}

		private int GetVariableIndex(SerializedProperty property) {
			string propertyPath = property.propertyPath;
			return Convert.ToInt32(propertyPath[(propertyPath.LastIndexOf('[') + 1)..propertyPath.LastIndexOf(']')]);
		}
	}
}
