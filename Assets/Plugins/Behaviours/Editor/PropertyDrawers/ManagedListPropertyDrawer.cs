using System;
using System.Collections.Generic;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	public abstract class ManagedListPropertyDrawer<T> : PropertyDrawer {
		private SerializedProperty m_listProperty;
		private List<SerializedProperty> m_listItemProperties = new();
		private List<Action> m_removeActions = new();

		private ListView m_listView;
		private VisualElement m_itemInspector;
		private Label m_inspectorLabel;

		protected void CreateListGUI(VisualElement rootVisualElement, SerializedProperty property, string createButtonText) {
			rootVisualElement.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			m_listProperty = property;
			ResetProperties();

			rootVisualElement.Add(m_listView = new ListView(m_listItemProperties) {
				name = "ManagedListView",
				makeItem = MakeListItem,
				bindItem = BindListItem,
				unbindItem = UnbindListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				selectionType = SelectionType.Single,
			});
			m_listView.selectedIndicesChanged += OnSelectedItemChanged;
			m_listView.itemIndexChanged += OnItemMoved;

			rootVisualElement.Add(new Button(CreateItem) {
				name = "CreateButton",
				text = createButtonText,
			});

			rootVisualElement.Add(m_itemInspector = new VisualElement() {
				name = "ManagedListInspector",
				style = { display = DisplayStyle.None },
			});
			m_itemInspector.Add(m_inspectorLabel = new Label());
		}

		private void ResetProperties() {
			m_listItemProperties.Clear();

			for (int i = 0; i < m_listProperty.arraySize; i++) {
				SerializedProperty variableProperty = m_listProperty.GetArrayElementAtIndex(i);
				m_listItemProperties.Add(variableProperty);
			}
		}

		private VisualElement MakeListItem() {
			VisualElement root = new VisualElement() {
				name = "ItemRoot",
			};

			root.Add(new Label());
			root.Add(new Button() { text = "-" });

			return root;
		}

		private void BindListItem(VisualElement element, int index) {
			if (m_removeActions.Count < index + 1)
				m_removeActions.Add(() => RemoveItemAtIndex(index));

			element.Q<Label>().text = m_listItemProperties[index].managedReferenceValue.GetType().GetDisplayOrTypeName();
			element.Q<Button>().clickable.clicked += m_removeActions[index];
		}

		private void UnbindListItem(VisualElement element, int index) {
			element.Q<Button>().clickable.clicked -= m_removeActions[index];
		}

		private void CreateItem() {
			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(typeof(T));

			TypeProvider.Instance.AskForType(mouseScreenPosition, types, type => {
				int nextIndex = m_listItemProperties.Count;

				m_listProperty.InsertArrayElementAtIndex(nextIndex);
				SerializedProperty newProperty = m_listProperty.GetArrayElementAtIndex(nextIndex);

				newProperty.managedReferenceValue = Activator.CreateInstance(type);
				newProperty.serializedObject.ApplyModifiedProperties();

				ResetProperties();
				m_listView.RefreshItems();
				m_listView.SetSelection(m_listItemProperties.Count - 1);
			});
		}

		private void OnSelectedItemChanged(IEnumerable<int> _) {
			if (m_listView.selectedIndex == -1)
				ClearInspection();
			else
				InspectItemWithIndex(m_listView.selectedIndex);
		}

		private void OnItemMoved(int from, int to) {
			ClearInspection();

			m_listProperty.MoveArrayElement(from, to);
			m_listProperty.serializedObject.ApplyModifiedProperties();

			ResetProperties();
			m_listView.RefreshItems();

			if (m_listView.selectedIndex != -1)
				InspectItemWithIndex(to);
		}

		private void RemoveItemAtIndex(int index) {
			if (index == m_listView.selectedIndex)
				ClearInspection();

			m_listItemProperties.RemoveAt(index);
			m_listProperty.DeleteArrayElementAtIndex(index);
			m_listProperty.serializedObject.ApplyModifiedProperties();

			ResetProperties();
			m_listView.RefreshItems();
		}

		private void InspectItemWithIndex(int index) {
			int childCount = m_itemInspector.childCount;
			for (int i = childCount - 1; i >= 1; i--)
				m_itemInspector.RemoveAt(i);

			SerializedProperty property = m_listItemProperties[index].Copy();

			m_inspectorLabel.text = property.managedReferenceValue.GetType().GetDisplayOrTypeName();

			int startDepth = property.depth;
			for (bool enterChildren = true; property.NextVisible(enterChildren) && property.depth > startDepth; enterChildren = false)
				m_itemInspector.Add(new PropertyField(property));

			m_itemInspector.Bind(property.serializedObject);
			m_itemInspector.style.display = DisplayStyle.Flex;
		}

		private void ClearInspection() {
			m_itemInspector.Unbind();

			int childCount = m_itemInspector.childCount;
			for (int i = childCount - 1; i >= 1; i--)
				m_itemInspector.RemoveAt(i);

			m_itemInspector.style.display = DisplayStyle.None;
			m_listView.selectedIndex = -1;
		}
	}
}
