using System;
using System.Collections.Generic;
using Jackey.Behaviours.FSM;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	public class StateTransitionGroupListPropertyDrawer : VisualElement {
		private SerializedProperty m_listProperty;

		private ListView m_groupsListView;
		private VisualElement m_groupInspector;

		private List<Action> m_removeActions = new();

		public StateTransitionGroupListPropertyDrawer() {
			RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			Add(m_groupsListView = new ListView() {
				name = "TransitionGroupListView",
				makeItem = MakeGroupListItem,
				bindItem = BindGroupListItem,
				unbindItem = UnbindGroupListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				showAddRemoveFooter = false,
				showBoundCollectionSize = false,
				selectionType = SelectionType.Single,
			});
			m_groupsListView.selectedIndicesChanged += OnSelectedGroupChanged;

			Add(m_groupInspector = new VisualElement() {
				name = "TransitionGroupInspector",
				style = { display = DisplayStyle.None },
			});
			m_groupInspector.Add(new PropertyField()); // Context
			m_groupInspector.Add(new PropertyField()); // Conditions

			Add(new Button(OnAddGroupClicked) {
				name = "CreateButton",
				text = "Add Group",
			});
		}

		public StateTransitionGroupListPropertyDrawer(SerializedProperty listProperty) : this() {
			Bind(listProperty);
		}

		public void Bind(SerializedProperty listProperty) {
			ClearGroupInspection();

			m_listProperty = listProperty.Copy();
			m_groupsListView.BindProperty(m_listProperty);
		}

		public void UnBind() {
			m_groupsListView.Unbind();
			m_listProperty = null;
		}

		private VisualElement MakeGroupListItem() {
			VisualElement root = new VisualElement() { name = "GroupRoot" };
			root.Add(new Label());
			root.Add(new Button() { text = "-"});
			return root;
		}

		private void BindGroupListItem(VisualElement element, int index) {
			if (m_removeActions.Count < index + 1)
				m_removeActions.Add(() => RemoveGroupAtIndex(index));

			SerializedProperty groupProperty = m_listProperty.GetArrayElementAtIndex(index);
			SerializedProperty contextProperty = groupProperty.FindPropertyRelative("m_context");

			Label label = element.Q<Label>();
			label.text = ObjectNames.NicifyVariableName(Enum.GetNames(typeof(StateTransitionContext))[contextProperty.enumValueIndex]);
			label.TrackPropertyValue(contextProperty, property => label.text = ObjectNames.NicifyVariableName(Enum.GetNames(typeof(StateTransitionContext))[property.enumValueIndex]));

			Button removeButton = element.Q<Button>();
			removeButton.clicked += m_removeActions[index];
		}

		private void UnbindGroupListItem(VisualElement element, int index) {
			element.Q<Label>().Unbind();
			element.Q<Button>().clicked -= m_removeActions[index];
		}

		private void OnSelectedGroupChanged(IEnumerable<int> _) {
			if (m_groupsListView.selectedIndex == -1) {
				ClearGroupInspection();
				return;
			}

			SerializedProperty groupProperty = m_listProperty.GetArrayElementAtIndex(m_groupsListView.selectedIndex);

			UQueryState<PropertyField> propertyFields = m_groupInspector.Query<PropertyField>().Build();
			propertyFields.AtIndex(0).BindProperty(groupProperty.FindPropertyRelative("m_context"));
			propertyFields.AtIndex(1).BindProperty(groupProperty.FindPropertyRelative("m_conditions"));

			m_groupInspector.style.display = DisplayStyle.Flex;
		}

		private void ClearGroupInspection() {
			m_groupsListView.SetSelection(-1);
			m_groupInspector.Unbind();
			m_groupInspector.style.display = DisplayStyle.None;
		}

		private void RemoveGroupAtIndex(int index) {
			int selectedIndex = m_groupsListView.selectedIndex;

			m_listProperty.DeleteArrayElementAtIndex(index);
			m_listProperty.serializedObject.ApplyModifiedProperties();

			if (index == selectedIndex)
				ClearGroupInspection();
			else if (index < selectedIndex)
				m_groupsListView.selectedIndex = selectedIndex - 1;
		}

		private void OnAddGroupClicked() {
			m_listProperty.InsertArrayElementAtIndex(m_listProperty.arraySize);
			m_listProperty.serializedObject.ApplyModifiedProperties();

			m_groupsListView.RefreshItems();
			m_groupsListView.SetSelection(m_listProperty.arraySize - 1);
		}
	}
}
