using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jackey.Behaviours.FSM.States;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	public class StateTransitionListPropertyDrawer : VisualElement {
		private SerializedProperty m_listProperty;

		private ListView m_transitionsListView;
		private StateTransitionGroupListPropertyDrawer m_groupsInspector;

		public StateTransitionListPropertyDrawer() {
			name = "TransitionList";
			RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			Add(new Label("Transitions"));

			Add(m_transitionsListView = new ListView() {
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

			Add(m_groupsInspector = new StateTransitionGroupListPropertyDrawer() {
				name = "TransitionListInspector",
				style = { display = DisplayStyle.None },
			});
		}

		public StateTransitionListPropertyDrawer(SerializedProperty listProperty) : this() {
			Bind(listProperty);
		}

		public void Bind(SerializedProperty listProperty) {
			ClearTransitionInspection();

			m_listProperty = listProperty.Copy();

			m_transitionsListView.BindProperty(m_listProperty);
			m_transitionsListView.TrackPropertyValue(m_listProperty, OnTransitionsPropertyChanged);
		}

		public void Unbind() {
			m_transitionsListView.Unbind();
			m_listProperty = null;
		}

		private VisualElement MakeTransitionListItem() {
			VisualElement root = new VisualElement() { name = "ItemRoot" };
			root.Add(new Label());
			return root;
		}

		private void BindTransitionListItem(VisualElement element, int index) {
			SerializedProperty transitionProperty = m_listProperty.GetArrayElementAtIndex(index);

			SerializedProperty destinationProperty = transitionProperty.FindPropertyRelative("m_destination");
			BehaviourState destination = (BehaviourState)destinationProperty.managedReferenceValue;

			string label;
			if (!string.IsNullOrWhiteSpace(destination.Name)) {
				label = destination.Name;
			}
			else {
				string destinationInfo = destination.Editor_Info;
				label = !string.IsNullOrEmpty(destinationInfo)
					? Regex.Replace(destinationInfo, "^(<.+?(?=>)>|\\W*)", string.Empty)
					: destination.GetType().GetDisplayOrTypeName();
			}

			element.Q<Label>().text = $"({index + 1}) {label}";
		}

		private void OnSelectedTransitionChanged(IEnumerable<int> _) {
			if (m_transitionsListView.selectedIndex == -1) {
				ClearTransitionInspection();
				return;
			}

			InspectTransitionAtIndex(m_transitionsListView.selectedIndex);
		}

		private void InspectTransitionAtIndex(int index) {
			m_transitionsListView.selectedIndex = index;

			m_groupsInspector.Bind(m_listProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_groups"));
			m_groupsInspector.style.display = DisplayStyle.Flex;
		}

		private void ClearTransitionInspection() {
			m_groupsInspector.Unbind();

			m_groupsInspector.style.display = DisplayStyle.None;
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
	}
}
