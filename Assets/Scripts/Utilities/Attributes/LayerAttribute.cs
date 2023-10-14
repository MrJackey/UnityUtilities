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
	/// Displays a LayerField in the inspector
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class LayerAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(LayerAttribute))]
		public class LayerPropertyDrawer : PropertyDrawer {
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (property.propertyType != SerializedPropertyType.Integer && property.propertyType != SerializedPropertyType.String) {
					Debug.LogWarning("The Layer attribute only supports application on int and string fields");
					return;
				}

				label = EditorGUI.BeginProperty(position, label, property);
				position = EditorGUI.PrefixLabel(position, label);

				string layerName = property.propertyType == SerializedPropertyType.Integer
					? LayerMask.LayerToName(property.intValue)
					: property.stringValue;

				int layer = property.propertyType == SerializedPropertyType.Integer
					? property.intValue
					: LayerMask.NameToLayer(layerName);

				if (Array.IndexOf(InternalEditorUtility.layers, layerName) == -1)
					GUI.backgroundColor = Color.yellow;

				EditorGUI.BeginChangeCheck();

				int layerFieldValue = EditorGUI.LayerField(position, layer);

				if (EditorGUI.EndChangeCheck()) {
					if (property.propertyType == SerializedPropertyType.Integer)
						property.intValue = layerFieldValue;
					else
						property.stringValue = LayerMask.LayerToName(layerFieldValue);
				}

				EditorGUI.EndProperty();
			}
		}
	}
#endif
}

