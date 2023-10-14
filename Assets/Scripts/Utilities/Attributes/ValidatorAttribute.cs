using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	///		<para>
	///		Add a validator to a field. It is invoked whenever the field value changes after OnValidate() is run.
	///		</para>
	///		<para>
	///		The validator must match the required signature
	///		<code>
	///		// Value and UnityObject.Object Types
	///		T ExampleValidator(T oldValue, T value)
	///		<br/>
	///		// Other Serialized Types
	///		void ExampleValidator(T value)
	///		<br/>
	///		</code>
	///		where T is the type of the field.
	///		</para>
	/// </summary>
	/// <remarks>
	///		Note that the validator is not run when values are pasted via the label context menu
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class ValidatorAttribute : PropertyAttribute {
		public string Validator { get; }

		public ValidatorAttribute(string validator) {
			Validator = validator;
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(ValidatorAttribute))]
		public class ValidatorAttributePropertyDrawer : PropertyDrawer {
			private static List<object> s_oldValues = new();
			private static object[] s_oldInclusiveParametersParameters = new object[2];
			private static object[] s_valueOnlyParameter = new object[1];

			private MethodInfo m_validator;

			private bool ShouldIncludeOldValue => fieldInfo.FieldType.IsValueType || !fieldInfo.FieldType.IsSerializable;

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUI.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (m_validator == null) {
					FindValidator(property);

					if (m_validator == null) {
						return;
					}
				}

				label = EditorGUI.BeginProperty(position, label, property);

				Object[] targetObjects = property.serializedObject.targetObjects;

				if (ShouldIncludeOldValue) {
					for (int i = 0; i < targetObjects.Length; i++) {
						if (i < s_oldValues.Count)
							s_oldValues[i] = fieldInfo.GetValue(targetObjects[i]);
						else
							s_oldValues.Add(fieldInfo.GetValue(targetObjects[i]));
					}
				}

				bool isExpanded = property.isExpanded;

				EditorGUI.BeginChangeCheck();

				EditorGUI.PropertyField(position, property, label, true);

				// ChangeCheck sees expanding/folding serializable classes as changes. These actions should not require any validation
				if (EditorGUI.EndChangeCheck() && isExpanded == property.isExpanded) {
					Undo.RecordObjects(targetObjects, $"Modified {property.displayName} in {(targetObjects.Length > 1 ? $"{targetObjects.Length} Objects" : property.serializedObject.targetObject.name)}");
					property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

					for (int i = 0; i < targetObjects.Length; i++) {
						Object targetObject = targetObjects[i];

						if (ShouldIncludeOldValue) {
							s_oldInclusiveParametersParameters[0] = s_oldValues[i];
							s_oldInclusiveParametersParameters[1] = fieldInfo.GetValue(targetObject);

							fieldInfo.SetValue(targetObject, m_validator.Invoke(targetObject, s_oldInclusiveParametersParameters));
						}
						else {
							s_valueOnlyParameter[0] = fieldInfo.GetValue(targetObject);
							m_validator.Invoke(targetObject, s_valueOnlyParameter);
						}
					}
				}

				EditorGUI.EndProperty();
			}

			private void FindValidator(SerializedProperty property) {
				ValidatorAttribute attr = (ValidatorAttribute)attribute;
				Type fieldType = fieldInfo.FieldType;

				Type targetType = property.serializedObject.targetObject.GetType();
				MethodInfo method = targetType.GetMethod(attr.Validator, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

				if (method == null) {
					Debug.LogWarning($"{nameof(ValidatorAttribute)} is unable to find validation method \"{attr.Validator}\" in {targetType}", property.serializedObject.targetObject);
					return;
				}

				ParameterInfo[] methodParameters = method.GetParameters();

				if (ShouldIncludeOldValue) {
					bool isMethodValid = method.ReturnParameter?.ParameterType == fieldType &&
					                     methodParameters.Length == 2 &&
					                     methodParameters.All(x => x.ParameterType == fieldType);

					if (!isMethodValid) {
						Debug.LogWarning($"Validation method \"{method.Name}\" in {targetType} does not match the required signature: Func<{fieldType}, {fieldType}, {fieldType}>", property.serializedObject.targetObject);
						return;
					}
				}
				else {
					bool isMethodValid = method.ReturnType == typeof(void) &&
					                     methodParameters.Length == 1 &&
					                     methodParameters[0].ParameterType == fieldType;

					if (!isMethodValid) {
						Debug.LogWarning($"Validation method \"{method.Name}\" in {targetType} does not match the required signature: Action<{fieldType}>", property.serializedObject.targetObject);
						return;
					}
				}

				m_validator = method;
			}
		}
	}
#endif
}
