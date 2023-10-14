using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Attributes {
	/// <summary>
	/// Requires an Object to be of a specific type.
	/// Especially useful for interfaces which normally aren't drag and droppable in the editor
	/// </summary>
	///
	/// <example>
	/// <code>
	/// [OfType(typeof(IExampleInterface))]
	///	[SerializeField] private MonoBehaviour m_exampleField;
	///  <br/>
	/// private void Start() {
	///		((IExampleInterface)m_exampleField).ExampleInterfaceMethod();
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public sealed class OfTypeAttribute : PropertyAttribute {
		public Type RequiredType { get; }

		public OfTypeAttribute(Type requiredType) {
			RequiredType = requiredType;
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(OfTypeAttribute))]
		public class OfTypePropertyDrawer : PropertyDrawer {
			private static readonly Color s_normalBackgroundColor = new(0.16f, 0.16f, 0.16f, 1f);
			private static readonly Color s_dragBackgroundColor = new(0.17f, 0.36f, 0.53f, 1f);

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				if (property.propertyType != SerializedPropertyType.ObjectReference) {
					Debug.LogWarning("The OfType attribute only supports application on UnityEngine.Object derived types");
					return;
				}

				OfTypeAttribute attr = (OfTypeAttribute)attribute;

				if (!attr.RequiredType.IsInterface && !typeof(Component).IsAssignableFrom(attr.RequiredType))
				{
					Debug.LogWarning("The OfType attribute only support requiring interfaces or Component derived types", property.serializedObject.targetObject);
					return;
				}

				label = EditorGUI.BeginProperty(position, label, property);
				Rect fieldRect = EditorGUI.PrefixLabel(position, label);

				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
				EditorGUI.indentLevel = 0;

				Object objectFieldValue = EditorGUI.ObjectField(fieldRect, GUIContent.none, property.objectReferenceValue, typeof(Object), true);
				bool objectFieldValueChanged = EditorGUI.EndChangeCheck();

				Color color = UpdateDragAndDrop(attr, fieldRect);

				if (!property.hasMultipleDifferentValues) {
					fieldRect.width = Mathf.Max(fieldRect.width - 22f, 0f);
					fieldRect.height -= 2f;
					fieldRect.y += 1f;
					fieldRect.x += 2f;

					EditorGUI.DrawRect(fieldRect, color);

					if (objectFieldValue) {
						Type valueType = objectFieldValue.GetType();
						GUIContent objectContent = EditorGUIUtility.ObjectContent(objectFieldValue, valueType);
						objectContent.text = $"{objectFieldValue.name} ({ObjectNames.NicifyVariableName(valueType.Name)})";

						GUI.Label(fieldRect, objectContent, EditorStyles.label);
					}
					else {
						GUI.Label(fieldRect, $"None ({ObjectNames.NicifyVariableName(fieldInfo.FieldType.Name)}+{attr.RequiredType.Name})", EditorStyles.label);
					}
				}

				if (!objectFieldValueChanged) {
					if (objectFieldValue != null && !attr.RequiredType.IsInstanceOfType(objectFieldValue))
						property.objectReferenceValue = null;

					EditorGUI.EndProperty();
					return;
				}

				if (!objectFieldValue || (attr.RequiredType.IsInstanceOfType(objectFieldValue) && fieldInfo.FieldType.IsInstanceOfType(objectFieldValue)))
					property.objectReferenceValue = objectFieldValue;
				else if (objectFieldValue is GameObject go && go.TryGetComponent(attr.RequiredType, out Component component))
					property.objectReferenceValue = component;

				EditorGUI.EndProperty();
			}

			private Color UpdateDragAndDrop(OfTypeAttribute attr, Rect fieldRect) {
				Event evt = Event.current;
				Object dragValue = DragAndDrop.objectReferences.FirstOrDefault();

				bool isMouseOver = fieldRect.Contains(evt.mousePosition);
				bool supportedDrag = isMouseOver && dragValue != null && IsObjectDroppable(attr, dragValue);

				if (isMouseOver)
					DragAndDrop.visualMode = supportedDrag ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

				if (supportedDrag)
					return s_dragBackgroundColor;

				return s_normalBackgroundColor;
			}

			private bool IsObjectDroppable(OfTypeAttribute attr, Object value)
			{
				if (attr.RequiredType.IsInstanceOfType(value) && fieldInfo.FieldType.IsInstanceOfType(value))
					return true;

				if (value is GameObject go && go.TryGetComponent(attr.RequiredType, out Component _))
					return true;

				return false;
			}
		}
	}
#endif
}
