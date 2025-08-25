using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jackey.Behaviours.FSM;
using Jackey.Behaviours.FSM.States;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(TransitionList), true)]
	public class TransitionListPropertyDrawer : PropertyDrawer {
		private SerializedProperty m_transitionsProperty;

		private ListView m_transitionsListView;
		private VisualElement m_transitionInspector;
		private ListView m_groupsListView;
		private VisualElement m_groupInspector;

		private List<Action> m_removeActions = new();

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement() { name = "TransitionList" };
			rootVisualElement.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			m_transitionsProperty = property.FindPropertyRelative("m_transitions");

			rootVisualElement.Add(m_transitionsListView = new ListView() {
				name = "TransitionListView",
				makeItem = MakeTransitionListItem,
				bindItem = BindTransitionListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				showAddRemoveFooter = false,
				showBoundCollectionSize = false,
				selectionType = SelectionType.Single,
			});
			m_transitionsListView.itemIndexChanged += OnTransitionMoved;
			m_transitionsListView.selectedIndicesChanged += OnSelectedTransitionChanged;
			m_transitionsListView.TrackPropertyValue(m_transitionsProperty, OnTransitionsPropertyChanged);

			m_transitionsListView.BindProperty(m_transitionsProperty);

			rootVisualElement.Add(m_transitionInspector = new VisualElement() {
				name = "TransitionListInspector",
				style = { display = DisplayStyle.None },
			});

			m_transitionInspector.Add(m_groupsListView = new ListView() {
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


			m_transitionInspector.Add(m_groupInspector = new VisualElement() {
				name = "GroupInspector",
				style = { display = DisplayStyle.None },
			});
			m_groupInspector.Add(new PropertyField()); // Context
			m_groupInspector.Add(new PropertyField()); // Conditions

			m_transitionInspector.Add(new Button(OnAddGroupClicked) {
				name = "CreateButton",
				text = "Add Group",
			});

			return rootVisualElement;
		}

		#region Transitions

		private VisualElement MakeTransitionListItem() {
			VisualElement root = new VisualElement() { name = "ItemRoot" };
			root.Add(new Label());
			return root;
		}

		private void BindTransitionListItem(VisualElement element, int index) {
			SerializedProperty transitionProperty = m_transitionsProperty.GetArrayElementAtIndex(index);

			SerializedProperty destinationProperty = transitionProperty.FindPropertyRelative("m_destination");
			BehaviourState destination = (BehaviourState)destinationProperty.managedReferenceValue;

			string label;
			if (!string.IsNullOrWhiteSpace(destination.Name)) {
				label = destination.Name;
			}
			else {
				string destinationInfo = destination.Editor_Info;
				label = !string.IsNullOrEmpty(destinationInfo)
					? Regex.Replace(destinationInfo, "^\\W*", string.Empty)
					: destination.GetType().Editor_GetDisplayOrTypeName();
			}

			element.Q<Label>().text = $"({index + 1}) {label}";
		}

		private void OnSelectedTransitionChanged(IEnumerable<int> _) {
			ClearGroupInspection();

			if (m_transitionsListView.selectedIndex == -1) {
				ClearTransitionInspection();
				return;
			}

			InspectTransitionAtIndex(m_transitionsListView.selectedIndex);
		}

		private void InspectTransitionAtIndex(int index) {
			m_transitionsListView.selectedIndex = index;

			m_groupsListView.BindProperty(GetGroupsProperty(index));
			m_transitionInspector.style.display = DisplayStyle.Flex;
		}

		private void ClearTransitionInspection() {
			m_groupsListView.Unbind();

			m_transitionInspector.style.display = DisplayStyle.None;
			m_transitionsListView.selectedIndex = -1;
		}

		private void OnTransitionMoved(int from, int to) {
			if (m_transitionsListView.selectedIndex != -1)
				InspectTransitionAtIndex(to);
			else
				ClearTransitionInspection();
		}

		private void OnTransitionsPropertyChanged(SerializedProperty property) {
			if (property.arraySize >= m_transitionsListView.itemsSource.Count) return;

			ClearTransitionInspection();
		}

		#endregion

		#region Groups

		private VisualElement MakeGroupListItem() {
			VisualElement root = new VisualElement() { name = "GroupRoot" };
			root.Add(new Label());
			root.Add(new Button() { text = "-"});
			return root;
		}

		private void BindGroupListItem(VisualElement element, int index) {
			if (m_removeActions.Count < index + 1)
				m_removeActions.Add(() => RemoveGroupAtIndex(index));

			SerializedProperty groupsProperty = GetGroupsProperty(m_transitionsListView.selectedIndex);
			SerializedProperty groupProperty = groupsProperty.GetArrayElementAtIndex(index);
			SerializedProperty contextProperty = groupProperty.FindPropertyRelative("m_context");

			Label label = element.Q<Label>();
			label.text = ObjectNames.NicifyVariableName(Enum.GetNames(typeof(StateTransitionContext))[contextProperty.enumValueIndex]);
			label.TrackPropertyValue(contextProperty, property => label.text = ObjectNames.NicifyVariableName(Enum.GetNames(typeof(StateTransitionContext))[property.enumValueIndex]));

			Button removeButton = element.Q<Button>();
			removeButton.clicked += m_removeActions[index];
			removeButton.style.display = groupsProperty.arraySize > 1 ? DisplayStyle.Flex : DisplayStyle.None;
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

			SerializedProperty groupProperty = GetGroupsProperty(m_transitionsListView.selectedIndex).GetArrayElementAtIndex(m_groupsListView.selectedIndex);

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
			SerializedProperty groupsProperty = GetGroupsProperty(m_transitionsListView.selectedIndex);
			int selectedIndex = m_groupsListView.selectedIndex;

			groupsProperty.DeleteArrayElementAtIndex(index);
			groupsProperty.serializedObject.ApplyModifiedProperties();

			if (index == selectedIndex)
				ClearGroupInspection();
			else if (index < selectedIndex)
				m_groupsListView.selectedIndex = selectedIndex - 1;
		}

		private void OnAddGroupClicked() {
			SerializedProperty groupsProperty = GetGroupsProperty(m_transitionsListView.selectedIndex);
			groupsProperty.InsertArrayElementAtIndex(groupsProperty.arraySize);
			m_transitionsProperty.serializedObject.ApplyModifiedProperties();

			m_groupsListView.RefreshItems();
			m_groupsListView.SetSelection(groupsProperty.arraySize - 1);
		}

		private SerializedProperty GetGroupsProperty(int transitionIndex) {
			return m_transitionsProperty.GetArrayElementAtIndex(transitionIndex).FindPropertyRelative("m_groups");
		}

		#endregion
	}
}
