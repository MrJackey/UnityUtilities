using System;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.Graph.FSM;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(BehaviourAction))]
	public class BehaviourActionPropertyDrawer : PropertyDrawer {
		private SerializedProperty m_property;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			m_property = property;
			bool hasAction = property.managedReferenceValue != null;

			VisualElement rootVisualElement = new VisualElement();

			Button selectButton = new Button(ChangeAction) {
				name = "SelectActionButton",
				text = hasAction ? "Change Action" : "Select Action",
				style = { marginTop = 5f, marginBottom = 5f },
			};
			rootVisualElement.Add(selectButton);

			if (hasAction) {
				VisualElement actionFields = new VisualElement() { name = "ActionFields" };
				rootVisualElement.Add(actionFields);

				actionFields.Add(new Label(property.managedReferenceValue.GetType().GetDisplayOrTypeName()) { name = "ActionFieldsHeader" });

				SerializedProperty iterator = property.Copy();
				int depth = iterator.depth;
				for (bool enterChildren = true; iterator.NextVisible(enterChildren) && iterator.depth >= depth; enterChildren = false) {
					PropertyField field = new PropertyField(iterator);
					actionFields.Add(field);

					if (iterator.name == "m_target")
						field.name = "TargetField";
				}
			}

			return rootVisualElement;
		}

		private void ChangeAction() {
			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

			TypeProvider.Instance.AskForType(mouseScreenPosition, FSMGraph.s_actionTypes, type => {
				m_property.managedReferenceValue = Activator.CreateInstance(type);
				m_property.serializedObject.ApplyModifiedProperties();
			});
		}
	}
}
