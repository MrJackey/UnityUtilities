using System;
using System.Diagnostics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Display a field with a custom name
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class DisplayNameAttribute : PropertyAttribute {
		public string Name { get; }

		public DisplayNameAttribute(string name) {
			Name = name;
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(DisplayNameAttribute))]
		public class DisplayNamePropertyDrawer : PropertyDrawer {
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUI.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				label.text = ((DisplayNameAttribute)attribute).Name;
				EditorGUI.PropertyField(position, property, label, true);
			}
		}
	}
#endif
}

