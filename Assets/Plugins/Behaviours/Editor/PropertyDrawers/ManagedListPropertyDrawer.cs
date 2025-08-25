using System;
using System.Collections.Generic;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	public class ManagedListPropertyDrawer : VisualElement {
		private SerializedProperty m_listProperty;
		private List<Action> m_removeActions = new();

		private ListView m_listView;
		private VisualElement m_itemInspector;
		private Label m_inspectorLabel;

		private Type[] m_createTypes;

		public ManagedListPropertyDrawer(SerializedProperty property, string createButtonText, Type[] createTypes) {
			RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			m_listProperty = property;
			m_createTypes = createTypes;

			Add(m_listView = new ListView() {
				name = "ManagedListView",
				makeItem = MakeListItem,
				bindItem = BindListItem,
				unbindItem = UnbindListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				showBoundCollectionSize = false,
				selectionType = SelectionType.Single,
			});
			m_listView.selectedIndicesChanged += OnSelectedItemChanged;
			m_listView.itemIndexChanged += OnItemMoved;
			m_listView.BindProperty(m_listProperty);

			Add(new Button(CreateItem) {
				name = "CreateButton",
				text = createButtonText,
			});

			Add(m_itemInspector = new VisualElement() {
				name = "ManagedListInspector",
				style = { display = DisplayStyle.None },
			});
			m_itemInspector.Add(m_inspectorLabel = new Label());
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

			element.Q<Label>().text = m_listProperty.GetArrayElementAtIndex(index).managedReferenceValue.GetType().GetDisplayOrTypeName();
			element.Q<Button>().clickable.clicked += m_removeActions[index];
		}

		private void UnbindListItem(VisualElement element, int index) {
			element.Q<Button>().clickable.clicked -= m_removeActions[index];
		}

		private void CreateItem() {
			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

			TypeProvider.Instance.AskForType(mouseScreenPosition, m_createTypes, type => {
				int nextIndex = m_listProperty.arraySize;

				m_listProperty.InsertArrayElementAtIndex(nextIndex);
				SerializedProperty newProperty = m_listProperty.GetArrayElementAtIndex(nextIndex);

				newProperty.managedReferenceValue = Activator.CreateInstance(type);
				newProperty.serializedObject.ApplyModifiedProperties();

				m_listView.RefreshItems();
				m_listView.SetSelection(nextIndex);
			});
		}

		private void OnSelectedItemChanged(IEnumerable<int> _) {
			if (m_listView.selectedIndex == -1)
				ClearInspection();
			else
				InspectItemWithIndex(m_listView.selectedIndex);
		}

		private void OnItemMoved(int from, int to) {
			if (m_listView.selectedIndex != -1)
				InspectItemWithIndex(to);
			else
				ClearInspection();
		}

		private void RemoveItemAtIndex(int index) {
			int selectedIndex = m_listView.selectedIndex;

			m_listProperty.DeleteArrayElementAtIndex(index);
			m_listProperty.serializedObject.ApplyModifiedProperties();

			if (index == selectedIndex)
				ClearInspection();
			else if (index < selectedIndex)
				m_listView.selectedIndex = selectedIndex - 1;
		}

		private void InspectItemWithIndex(int index) {
			int childCount = m_itemInspector.childCount;
			for (int i = childCount - 1; i >= 1; i--)
				m_itemInspector.RemoveAt(i);

			SerializedProperty property = m_listProperty.GetArrayElementAtIndex(index);

			m_inspectorLabel.text = property.managedReferenceValue.GetType().GetDisplayOrTypeName();

			int startDepth = property.depth;
			for (bool enterChildren = true; property.NextVisible(enterChildren) && property.depth > startDepth; enterChildren = false) {
				PropertyField field = new PropertyField(property);
				m_itemInspector.Add(field);

				if (property.name == "m_target")
					field.name = "TargetField";
			}

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
