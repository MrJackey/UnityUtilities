using System;
using System.Collections.Generic;
using System.Reflection;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Utilities;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(BlackboardRef<>))]
	public class BlackboardRefPropertyDrawer : PropertyDrawer {
		private static Texture s_fieldIcon = EditorGUIUtility.IconContent("InputField Icon").image;
		private static Texture s_blackboardIcon = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;

		private static List<string> s_dropdownOptions = new();
		private static List<SerializedGUID> s_dropdownValues = new();

		private bool m_blackboardOnly;
		private bool m_canEditField;

		private SerializedProperty m_property;
		private Type m_refType;

		private VisualElement m_root;
		private VisualElement m_fieldRow;
		private Label m_convertLabel;

		private SerializedProperty m_fieldProperty;
		private PropertyField m_propertyField;

		private VisualElement m_noEditField;
		private Label m_noEditLabel;
		private Label m_noEditValue;

		private DropdownField m_dropdownField;
		private SerializedProperty m_variableGuidProperty;
		private SerializedProperty m_variableNameProperty;

		private SerializedProperty m_modeProperty;
		private Image m_modeImage;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			if (!EditorWindow.HasOpenInstances<BehaviourEditorWindow>())
				return null;

			m_property = property;

			m_root = new VisualElement() {
				name = "BlackboardRef",
			};

			m_fieldRow = new VisualElement() {
				name = "FieldRow",
			};
			m_root.Add(m_fieldRow);

			m_blackboardOnly = fieldInfo.GetCustomAttribute(typeof(BlackboardOnlyAttribute)) != null;
			m_canEditField = !fieldInfo.FieldType.GetGenericArguments()[0].IsInterface;

			SerializedProperty behaviourProperty = property.FindPropertyRelative("m_behaviour");
			behaviourProperty.objectReferenceValue ??= EditorWindow.GetWindow<BehaviourEditorWindow>().OpenBehaviour;
			property.serializedObject.ApplyModifiedProperties();

			m_modeProperty = property.FindPropertyRelative("m_mode");

			if (m_blackboardOnly && m_modeProperty.enumValueIndex != 1) {
				m_modeProperty.enumValueIndex = 1;
				m_modeProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}

			int mode = m_modeProperty.enumValueIndex;

			m_fieldProperty = property.FindPropertyRelative("m_fieldValue");
			m_propertyField = new PropertyField(m_fieldProperty, property.displayName);

			if (!m_canEditField) {
				m_noEditField = new VisualElement();
				m_noEditField.AddToClassList("unity-base-field");
				m_noEditField.AddToClassList("unity-property-field");

				m_noEditLabel = new Label(property.displayName);
				m_noEditLabel.AddToClassList("unity-base-field__label");

				m_noEditValue = new Label(fieldInfo.FieldType.GetGenericArguments()[0].Name);
				m_noEditValue.AddToClassList("unity-base-field__input");
				m_noEditValue.AddToClassList("NoEditValue");

				m_noEditField.Add(m_noEditLabel);
				m_noEditField.Add(m_noEditValue);
			}

			m_variableGuidProperty = property.FindPropertyRelative("m_variableGuid");
			SerializedGUID guidValue = SerializedGUID.Editor_GetFromProperty(m_variableGuidProperty);
			m_variableNameProperty = property.FindPropertyRelative("m_variableName");
			string variableName = m_variableNameProperty.stringValue;

			m_refType = fieldInfo.FieldType.GenericTypeArguments[0];
			BlackboardVar referencedVariable = FindVariable(guidValue, variableName);

			m_dropdownField = new DropdownField() {
				label = property.displayName,
				choices = s_dropdownOptions,
				value = string.Empty,
			};

			m_convertLabel = new Label() {
				name = "ConvertLabel",
			};

			if (referencedVariable != null) {
				RefreshDropdownOptions();

				if (referencedVariable.IsAssignableTo(m_refType)) {
					m_dropdownField.SetValueWithoutNotify(referencedVariable.Name);
				}
			}
			else {
				m_dropdownField.SetValueWithoutNotify(guidValue == default ? "" : variableName);
			}

			RefreshConvertLabel(referencedVariable);

			// Trickle down to catch the event before the dropdown field does
			m_dropdownField.RegisterCallback<PointerDownEvent>(OnDropdownPointerDown, TrickleDown.TrickleDown);
			m_dropdownField.RegisterValueChangedCallback(OnDropdownValueChanged);

			if (mode == 0)
				m_fieldRow.Add(m_canEditField ? m_propertyField : m_noEditField);
			else
				m_fieldRow.Add(m_dropdownField);

			if (!m_blackboardOnly) {
				Button modeButton = new Button(ToggleMode) { name = "ModeButton" };
				modeButton.Add(m_modeImage = new Image() {
					image = mode == 0 ? s_fieldIcon : s_blackboardIcon,
					scaleMode = ScaleMode.ScaleAndCrop,
				});
				m_fieldRow.Add(modeButton);
			}

			return m_root;
		}

		[CanBeNull]
		private BlackboardVar FindVariable(SerializedGUID guid, string name) {
			foreach (Blackboard blackboard in Blackboard.Available) {
				if (blackboard == null) continue;

				BlackboardVar variable = blackboard.FindVariableWithGuidOrName(guid, name);
				if (variable != null)
					return variable;
			}

			return null;
		}

		private void RefreshDropdownOptions() {
			s_dropdownOptions.Clear();
			s_dropdownValues.Clear();

			SerializedGUID referencedGuid = SerializedGUID.Editor_GetFromProperty(m_variableGuidProperty);
			bool missingReference = referencedGuid == default;

			foreach (Blackboard blackboard in Blackboard.Available) {
				if (blackboard == null) continue;

				foreach (BlackboardVar variable in blackboard.m_variables) {
					if (!variable.IsAssignableTo(m_refType)) continue;

					if (referencedGuid == variable.Guid)
						missingReference = false;

					s_dropdownOptions.Add(variable.Name);
					s_dropdownValues.Add(variable.Guid);
				}
			}

			s_dropdownOptions.Add(string.Empty);
			s_dropdownOptions.Add("[Clear Reference]");

			if (missingReference) {
				m_dropdownField.value = string.Empty;
			}
		}

		private void OnDropdownPointerDown(PointerDownEvent evt) => RefreshDropdownOptions();
		private void OnDropdownValueChanged(ChangeEvent<string> _) {
			if (m_dropdownField.index == s_dropdownValues.Count + 1) { // Clear
				SerializedGUID.Editor_WriteToProperty(m_variableGuidProperty, new SerializedGUID());
				m_dropdownField.SetValueWithoutNotify(string.Empty);
			}
			else {
				SerializedGUID.Editor_WriteToProperty(m_variableGuidProperty, s_dropdownValues[m_dropdownField.index]);
			}

			m_variableGuidProperty.serializedObject.ApplyModifiedProperties();

			RefreshConvertLabel();
		}

		private void ToggleMode() {
			int oldValue = m_modeProperty.enumValueIndex;
			int newValue = oldValue == 0 ? 1 : 0;

			m_modeImage.image = newValue == 0 ? s_fieldIcon : s_blackboardIcon;
			m_modeProperty.enumValueIndex = newValue;
			m_modeProperty.serializedObject.ApplyModifiedProperties();

			if (newValue == 0) {
				m_dropdownField.RemoveFromHierarchy();

				if (m_canEditField) {
					m_propertyField.BindProperty(m_fieldProperty);
					m_fieldRow.Insert(0, m_propertyField);
				}
				else {
					m_fieldRow.Insert(0, m_noEditField);
				}

				m_convertLabel.RemoveFromHierarchy();
			}
			else {
				if (m_canEditField) {
					m_propertyField.RemoveFromHierarchy();
					m_propertyField.Unbind();
				}
				else {
					m_noEditField.RemoveFromHierarchy();
				}

				m_fieldRow.Insert(0, m_dropdownField);
				RefreshConvertLabel();
			}
		}

		private void RefreshConvertLabel() {
			string variableName = m_variableNameProperty.stringValue;
			BlackboardVar referencedVariable = FindVariable(SerializedGUID.Editor_GetFromProperty(m_variableGuidProperty), variableName);

			RefreshConvertLabel(referencedVariable);
		}

		private void RefreshConvertLabel(BlackboardVar referencedVariable) {
			if (referencedVariable == null || m_modeProperty.enumValueIndex == 0) {
				m_convertLabel.RemoveFromHierarchy();
				return;
			}

			Type variableType = referencedVariable.GetSerializedType();
			Debug.Assert(variableType != null);

			if (BlackboardConverter.IsConvertible(variableType, m_refType)) {
				m_convertLabel.text = $"{variableType.Name} => {m_refType.Name}";

				if (m_convertLabel.parent == null)
					m_root.Add(m_convertLabel);
			}
			else {
				m_convertLabel.RemoveFromHierarchy();
			}
		}
	}
}
