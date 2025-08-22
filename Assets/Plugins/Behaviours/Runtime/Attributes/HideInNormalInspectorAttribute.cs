using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Jackey.Behaviours.Attributes {
	/// <summary>
	/// Hides a field in the inspector but keeps it visible in the debug view
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class HideInNormalInspectorAttribute : PropertyAttribute { }

	#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(HideInNormalInspectorAttribute))]
		public class HideInNormalInspectorPropertyDrawer : PropertyDrawer {
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;
		}
	}
	#endif
}
