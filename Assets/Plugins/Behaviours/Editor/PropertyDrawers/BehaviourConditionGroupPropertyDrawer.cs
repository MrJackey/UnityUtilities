using System;
using System.Collections.Generic;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.Editor.TypeSearch;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(BehaviourConditionGroup))]
	public class BehaviourConditionGroupPropertyDrawer : PropertyDrawer {
		private SerializedProperty m_conditionsProperty;
		private List<SerializedProperty> m_conditionProperties = new();
		private List<Action> m_removeActions = new();

		private ListView m_listView;
		private VisualElement m_conditionInspector;
		private Label m_conditionInspectorLabel;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();
			rootVisualElement.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			SerializedProperty invertProperty = property.FindPropertyRelative("m_invert");
			rootVisualElement.Add(new PropertyField(invertProperty));

			SerializedProperty policyProperty = property.FindPropertyRelative("m_policy");
			rootVisualElement.Add(new PropertyField(policyProperty, string.Empty));

			m_conditionsProperty = property.FindPropertyRelative("m_conditions");
			ResetProperties();

			rootVisualElement.Add(m_listView = new ListView(m_conditionProperties) {
				name = "ConditionListView",
				makeItem = MakeListItem,
				bindItem = BindListItem,
				unbindItem = UnbindListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				selectionType = SelectionType.Single,
			});
			m_listView.selectedIndicesChanged += OnSelectedConditionChanged;
			m_listView.itemIndexChanged += OnConditionMoved;

			rootVisualElement.Add(new Button(CreateCondition) {
				name = "CreateButton",
				text = "Add Condition",
			});

			rootVisualElement.Add(m_conditionInspector = new VisualElement() {
				name = "ConditionInspector",
				style = { display = DisplayStyle.None },
			});
			m_conditionInspector.Add(m_conditionInspectorLabel = new Label());

			return rootVisualElement;
		}

		private void ResetProperties() {
			m_conditionProperties.Clear();

			for (int i = 0; i < m_conditionsProperty.arraySize; i++) {
				SerializedProperty variableProperty = m_conditionsProperty.GetArrayElementAtIndex(i);
				m_conditionProperties.Add(variableProperty);
			}
		}

		private VisualElement MakeListItem() {
			VisualElement root = new VisualElement() {
				name = "ConditionRoot",
			};

			root.Add(new Label());
			root.Add(new Button() { text = "-" });

			return root;
		}

		private void BindListItem(VisualElement element, int index) {
			if (m_removeActions.Count < index + 1)
				m_removeActions.Add(() => RemoveCondition(index));

			element.Q<Label>().text = ObjectNames.NicifyVariableName(m_conditionProperties[index].managedReferenceValue.GetType().Name);
			element.Q<Button>().clickable.clicked += m_removeActions[index];
		}

		private void UnbindListItem(VisualElement element, int index) {
			element.Q<Button>().clickable.clicked -= m_removeActions[index];
		}

		private void CreateCondition() {
			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			TypeCache.TypeCollection conditionTypes = TypeCache.GetTypesDerivedFrom(typeof(BehaviourCondition));

			TypeProvider.Instance.AskForType(mouseScreenPosition, conditionTypes, type => {
				int nextIndex = m_conditionProperties.Count;

				m_conditionsProperty.InsertArrayElementAtIndex(nextIndex);
				SerializedProperty newProperty = m_conditionsProperty.GetArrayElementAtIndex(nextIndex);

				newProperty.managedReferenceValue = Activator.CreateInstance(type);
				newProperty.serializedObject.ApplyModifiedProperties();

				ResetProperties();
				m_listView.RefreshItems();
				m_listView.SetSelection(m_conditionProperties.Count - 1);
			});
		}

		private void OnSelectedConditionChanged(IEnumerable<int> _) {
			if (m_listView.selectedIndex == -1)
				ClearInspection();
			else
				InspectCondition(m_listView.selectedIndex);
		}

		private void OnConditionMoved(int from, int to) {
			ClearInspection();

			m_conditionsProperty.MoveArrayElement(from, to);
			m_conditionsProperty.serializedObject.ApplyModifiedProperties();

			ResetProperties();
			m_listView.RefreshItems();

			if (m_listView.selectedIndex != -1)
				InspectCondition(to);
		}

		private void RemoveCondition(int index) {
			if (index == m_listView.selectedIndex)
				ClearInspection();

			m_conditionProperties.RemoveAt(index);
			m_conditionsProperty.DeleteArrayElementAtIndex(index);
			m_conditionsProperty.serializedObject.ApplyModifiedProperties();

			ResetProperties();
			m_listView.RefreshItems();
		}

		private void InspectCondition(int index) {
			int childCount = m_conditionInspector.childCount;
			for (int i = childCount - 1; i >= 1; i--)
				m_conditionInspector.RemoveAt(i);

			SerializedProperty property = m_conditionProperties[index].Copy();

			m_conditionInspectorLabel.text = ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name);

			int startDepth = property.depth;
			for (bool enterChildren = true; property.NextVisible(enterChildren) && property.depth > startDepth; enterChildren = false)
				m_conditionInspector.Add(new PropertyField(property));

			m_conditionInspector.Bind(property.serializedObject);
			m_conditionInspector.style.display = DisplayStyle.Flex;
		}

		private void ClearInspection() {
			m_conditionInspector.Unbind();

			int childCount = m_conditionInspector.childCount;
			for (int i = childCount - 1; i >= 1; i--)
				m_conditionInspector.RemoveAt(i);

			m_conditionInspector.style.display = DisplayStyle.None;
			m_listView.selectedIndex = -1;
		}
	}
}
