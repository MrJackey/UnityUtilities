using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Displays a TagField in the inspector
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class TagAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(TagAttribute))]
		public class TagPropertyDrawer : PropertyDrawer {
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUI.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (property.propertyType != SerializedPropertyType.String) {
					Debug.LogWarning("The Tag attribute only supports application on string fields");
					return;
				}

				label = EditorGUI.BeginProperty(position, label, property);
				position = EditorGUI.PrefixLabel(position, label);

				if (Array.IndexOf(InternalEditorUtility.tags, property.stringValue) == -1)
					GUI.backgroundColor = Color.yellow;

				EditorGUI.BeginChangeCheck();
				string tagFieldValue = EditorGUI.TagField(position, property.stringValue);
				if (EditorGUI.EndChangeCheck())
					property.stringValue = tagFieldValue;

				EditorGUI.EndProperty();
			}
		}
	}
#endif
}
