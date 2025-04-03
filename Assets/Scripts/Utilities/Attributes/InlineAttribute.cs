using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Draw all serialized fields of an object on one line
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public class InlineAttribute : PropertyAttribute {
		public bool PrefixLabel { get; set; } = true;
		public bool FieldLabels { get; set; }
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(InlineAttribute))]
		public class InlinePropertyDrawer : PropertyDrawer {
			private bool m_labelsInitialized;
			private GUIContent[] m_subLabels;

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				InlineAttribute attr = (InlineAttribute)attribute;

				EditorGUI.BeginProperty(position, label, property);

				if (!property.hasVisibleChildren) {
					EditorGUI.PropertyField(position, property, attr.PrefixLabel ? label : GUIContent.none, true);
					EditorGUI.EndProperty();
					return;
				}

				if (!m_labelsInitialized)
					InitializeLabels(property);

				if (attr.PrefixLabel)
					position = EditorGUI.PrefixLabel(position, label);

				property.NextVisible(true);
				EditorGUI.MultiPropertyField(position, m_subLabels, property, GUIContent.none);

				EditorGUI.EndProperty();
			}

			private void InitializeLabels(SerializedProperty property) {
				InlineAttribute attr = (InlineAttribute)attribute;

				SerializedProperty propertyIterator = property.Copy();

				int propertyDepth = propertyIterator.depth + 1;

				List<GUIContent> subLabels = new();

				for (bool enterChildren = true; propertyIterator.NextVisible(enterChildren); enterChildren = false) {
					if (propertyIterator.depth != propertyDepth) break;

					if (attr.FieldLabels)
						subLabels.Add(new GUIContent(ObjectNames.NicifyVariableName(propertyIterator.name)));
					else
						subLabels.Add(GUIContent.none);
				}

				m_subLabels = subLabels.ToArray();
				m_labelsInitialized = true;
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				float maxHeight = EditorGUIUtility.singleLineHeight;

				if (property.hasVisibleChildren) {
					int basePropertyDepth = property.depth;

					for (bool enterChildren = true; property.NextVisible(enterChildren) && property.depth > basePropertyDepth; enterChildren = false) {
						maxHeight = Mathf.Max(maxHeight, EditorGUI.GetPropertyHeight(property, true));
					}
				}
				else {
					maxHeight = EditorGUI.GetPropertyHeight(property, true);
				}

				return maxHeight + EditorGUIUtility.standardVerticalSpacing;
			}
		}
	}
#endif
}
