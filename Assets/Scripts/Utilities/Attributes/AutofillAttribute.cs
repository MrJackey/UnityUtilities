using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Looks for a Component to fill the field whenever its value is null.
	/// <br/><br/>
	///	Note that this is only active in editor edit mode to prevent any effect on gameplay
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class AutofillAttribute : PropertyAttribute {
		public Hierarchy HierarchyMode { get; }

		public AutofillAttribute(Hierarchy hierarchyMode = Hierarchy.GameObject) {
			HierarchyMode = hierarchyMode;
		}

		public enum Hierarchy {
			/// <summary>
			/// Search only the game object that the field's component is located on
			/// </summary>
			GameObject,

			/// <summary>
			/// Search the field's component's game object and any of its children
			/// </summary>
			Children,

			/// <summary>
			/// Search the field's component's game object and any of its ancestors
			/// </summary>
			Ancestors,

			/// <summary>
			/// Search the field's component's game object and both its children and ancestors.
			/// Children are prioritized over ancestors
			/// </summary>
			Complete,
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(AutofillAttribute))]
		public class AutofillPropertyDrawer : PropertyDrawer {
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUI.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				bool isSearchValid = !EditorApplication.isPlaying &&
				                     !property.serializedObject.isEditingMultipleObjects &&
				                     !property.objectReferenceValue;

				if (property.serializedObject.targetObject is ScriptableObject) {
					Debug.LogWarning($"The Autofill attribute only supports usage in components", property.serializedObject.targetObject);
					isSearchValid = false;
				}


				if (property.propertyType != SerializedPropertyType.ObjectReference) {
					Debug.LogWarning("The Autofill attribute only supports application on UnityEngine.Component or interface fields");
					isSearchValid = false;
				}

				Type fieldType = fieldInfo.FieldType;
				if (!fieldType.IsInterface && !typeof(Component).IsAssignableFrom(fieldType)) {
					Debug.LogWarning("The Autofill attribute only supports application on UnityEngine.Component or interface fields");
					isSearchValid = false;
				}

				label = EditorGUI.BeginProperty(position, label, property);

				EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

				if (isSearchValid)
					Search(property);

				EditorGUI.PropertyField(position, property, label, true);
				EditorGUI.EndProperty();
			}

			private void Search(SerializedProperty property) {
				AutofillAttribute attr = (AutofillAttribute)attribute;

				Component component = (Component)property.serializedObject.targetObject;
				Type fieldType = fieldInfo.FieldType;

				Object value;

				switch (attr.HierarchyMode) {
					case AutofillAttribute.Hierarchy.GameObject:
						value = component.GetComponent(fieldType);
						break;

					case AutofillAttribute.Hierarchy.Children:
						value = component.GetComponentInChildren(fieldType);
						break;

					case AutofillAttribute.Hierarchy.Ancestors:
						value = component.GetComponentInParent(fieldType);
						break;

					case AutofillAttribute.Hierarchy.Complete:
						Component childrenValue = component.GetComponentInChildren(fieldType);
						value = childrenValue
							? childrenValue
							: component.GetComponentInParent(fieldType);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				if (value != null) {
					Object targetObject = property.serializedObject.targetObject;
					Debug.Log($"[AutoFill] Assigning \"{property.displayName}\" (Component: {ObjectNames.NicifyVariableName(targetObject.GetType().Name)}, Value: {value})", targetObject);
				}

				property.objectReferenceValue = value;
			}
		}
	}
	#endif
}

