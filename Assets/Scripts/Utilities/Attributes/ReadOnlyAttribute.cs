using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Prevents editing this field in the inspector in the specified environment
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class ReadOnlyAttribute : PropertyAttribute {
		public Environment Env { get; }

		public ReadOnlyAttribute(Environment environment = Environment.Always) {
			Env = environment;
		}

		public enum Environment {
			Always,
			PlayMode,
			EditMode,
			PlayModeAndEnabled,
			EditModeAndEnabled,
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
		public class ReadOnlyPropertyDrawer : PropertyDrawer {
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUI.GetPropertyHeight(property, label);
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				bool condition = ((ReadOnlyAttribute)attribute).Env switch {
					ReadOnlyAttribute.Environment.Always => true,
					ReadOnlyAttribute.Environment.PlayMode => EditorApplication.isPlaying,
					ReadOnlyAttribute.Environment.EditMode => !EditorApplication.isPlaying,
					ReadOnlyAttribute.Environment.PlayModeAndEnabled => EditorApplication.isPlaying &&
					                                                    (property.serializedObject.targetObject is not MonoBehaviour ||
					                                                     property.serializedObject.targetObjects.All(@object => ((MonoBehaviour)@object).isActiveAndEnabled)),
					ReadOnlyAttribute.Environment.EditModeAndEnabled => !EditorApplication.isPlaying &&
					                                                    (property.serializedObject.targetObject is not MonoBehaviour ||
					                                                     property.serializedObject.targetObjects.All(@object => ((MonoBehaviour)@object).isActiveAndEnabled)),
					_ => throw new ArgumentOutOfRangeException(),
				};

				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.BeginDisabledGroup(condition);

				EditorGUI.PropertyField(position, property, label, true);

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndProperty();
			}
		}
	}
#endif
}
