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
	/// Expose the properties of the assigned object directly in the inspector
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class ExposeObjectAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(ExposeObjectAttribute))]
		public class ExposeObjectPropertyDrawer : PropertyDrawer {
			private readonly Color FOLDOUT_GUIDE_COLOR = new(0.39f, 0.4f, 0.39f);

			private SerializedObject m_exposedSerializedObject;
			private float m_propertyHeight;

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				// Object Field
				float totalHeight = EditorGUIUtility.singleLineHeight;

				if (!property.isExpanded || property.objectReferenceValue == null)
					return totalHeight;

				// HelpBox
				if (property.hasMultipleDifferentValues)
					totalHeight += 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

				// Object Properties
				if (!property.hasMultipleDifferentValues && m_exposedSerializedObject != null) {
					SerializedProperty iterator = m_exposedSerializedObject.GetIterator();

					for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
						totalHeight += EditorGUI.GetPropertyHeight(iterator, true);
					}

					// Extra Padding
					totalHeight += 4f * EditorGUIUtility.standardVerticalSpacing;
				}

				m_propertyHeight = totalHeight;
				return totalHeight;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				position.height = EditorGUIUtility.singleLineHeight;

				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.PropertyField(position, property, label, true);
				EditorGUI.EndProperty();

				if (property.propertyType != SerializedPropertyType.ObjectReference) {
					Debug.LogWarning("The ExposeObject attribute only supports application on UnityEngine.Object derived types");
					return;
				}

				Object propertyValue = property.objectReferenceValue;

				if (propertyValue == null) {
					return;
				}

				property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none, true);

				if (property.isExpanded) {
					if (m_exposedSerializedObject == null || m_exposedSerializedObject.targetObject != propertyValue)
						m_exposedSerializedObject = new SerializedObject(propertyValue);

					const float FOLDOUT_X_OFFSET = 7f;

					Rect foldGuideRect = position;
					foldGuideRect.x -= FOLDOUT_X_OFFSET;
					foldGuideRect.y += EditorGUIUtility.singleLineHeight;
					foldGuideRect.width = 1f;
					foldGuideRect.height = m_propertyHeight - EditorGUIUtility.singleLineHeight;
					EditorGUI.DrawRect(foldGuideRect, FOLDOUT_GUIDE_COLOR);

					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(!GUI.enabled);

					Rect helpRect = EditorGUI.IndentedRect(position);
					helpRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					helpRect.height *= 2f;

					if (property.hasMultipleDifferentValues)
						EditorGUI.HelpBox(helpRect, "Unable to view/edit properties whilst editing multiple objects with different values", MessageType.Info);
					else
						DrawExposedProperties(position);

					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}
			}

			private void DrawExposedProperties(Rect position) {
				m_exposedSerializedObject.Update();

				SerializedProperty iterator = m_exposedSerializedObject.GetIterator();

				for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
					if (iterator.name == "m_Script")
						EditorGUI.BeginDisabledGroup(true);

					position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					position.height = EditorGUI.GetPropertyHeight(iterator, true);

					EditorGUI.PropertyField(position, iterator, true);
					position.y += position.height - EditorGUIUtility.singleLineHeight;

					if (iterator.name == "m_Script") {
						EditorGUI.EndDisabledGroup();
					}
				}

				m_exposedSerializedObject.ApplyModifiedProperties();
			}
		}
	}
#endif
}

