using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Clamps animation curve keys within a specified rect
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class CurveSettingsAttribute : PropertyAttribute {
		public Vector2 Min { get; }
		public Vector2 Max { get; }

		public CurveSettingsAttribute(float minTime, float maxTime, float minValue, float maxValue) {
			Min = new Vector2(minTime, minValue);
			Max = new Vector2(maxTime, maxValue);
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(CurveSettingsAttribute))]
		public class CurveSettingsPropertyDrawer : PropertyDrawer {
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (property.propertyType != SerializedPropertyType.AnimationCurve) {
					Debug.LogWarning("The CurveSettings attribute only supports application on UnityEngine.AnimationCurve fields");
					return;
				}

				CurveSettingsAttribute attr = (CurveSettingsAttribute)attribute;
				Rect curveRect = new Rect(attr.Min, attr.Max - attr.Min);

				label = EditorGUI.BeginProperty(position, label, property);
				position = EditorGUI.PrefixLabel(position, label);

				EditorGUI.BeginChangeCheck();
				AnimationCurve curveFieldValue = EditorGUI.CurveField(position, property.animationCurveValue, Color.green, curveRect);
				if (EditorGUI.EndChangeCheck())
					property.animationCurveValue = curveFieldValue;

				EditorGUI.EndProperty();
			}
		}
	}
#endif
}

