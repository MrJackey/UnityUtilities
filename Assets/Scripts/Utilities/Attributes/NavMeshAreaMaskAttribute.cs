using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Displays a Mask field of all NavMesh areas in the inspector
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class NavMeshAreaMaskAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(NavMeshAreaMaskAttribute))]
		public class NavMeshAreaPropertyDrawer : PropertyDrawer {
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUI.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (property.propertyType != SerializedPropertyType.Integer) {
					Debug.LogWarning("The NavMeshAreaMask attribute only supports application on int fields");
					return;
				}

				label = EditorGUI.BeginProperty(position, label, property);

				EditorGUI.BeginChangeCheck();
				int maskFieldValue = EditorGUI.MaskField(position, label, property.intValue, GameObjectUtility.GetNavMeshAreaNames());
				if (EditorGUI.EndChangeCheck())
					property.intValue = maskFieldValue;

				EditorGUI.EndProperty();
			}
		}
	}
#endif
}

