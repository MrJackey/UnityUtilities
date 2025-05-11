using System;
using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Editor.ObjectPicker;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.EditorWindows {
	public class DatabaseWindow : EditorWindow {
		[SerializeField] private StyleSheet m_searchStyleSheet;
		[SerializeField] private StyleSheet m_windowStyleSheet;

		private List<GlobalObjectAsset> m_assetList;
		private ListView m_listView;

		private readonly List<EventCallback<ChangeEvent<string>>> m_nameCallbacks = new();
		private readonly List<EventCallback<ChangeEvent<string>>> m_descriptionCallbacks = new();
		private readonly List<Action> m_deleteActions = new();

		[MenuItem("Tools/Jackey/Global References/Database", priority = 1000)]
		private static void ShowWindow() {
			DatabaseWindow window = GetWindow<DatabaseWindow>();
			window.Show();
		}

		private void CreateGUI() {
			titleContent = new GUIContent("Global Object Database");

			rootVisualElement.styleSheets.Add(m_searchStyleSheet);
			rootVisualElement.styleSheets.Add(m_windowStyleSheet);

			m_assetList = new List<GlobalObjectAsset>();
			m_listView = new ListView() {
				makeItem = CreateListItem,
				bindItem = BindListItem,
				unbindItem = UnbindListItem,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				reorderable = false,
				showAddRemoveFooter = false,
				selectionType = SelectionType.None,
				fixedItemHeight = 46f,
			};

			rootVisualElement.Add(new GlobalObjectPicker(m_listView, m_assetList));
			m_listView.itemsSource = m_assetList;

			VisualElement header = new VisualElement() { name = "TableHeader" };

			header.Add(new Label("GUID"));
			header.Add(new Label("Name"));
			header.Add(new Label("Description"));

			rootVisualElement.Add(header);
			rootVisualElement.Add(m_listView);
		}

		private VisualElement CreateListItem() {
			VisualElement itemRootElement = new VisualElement() { name = "ListItem" };

			TextField guidField = new TextField() { name = "GUIDField" };
			guidField.SetEnabled(false);
			itemRootElement.Add(guidField);

			itemRootElement.Add(new TextField() { name = "NameField", isDelayed = true });
			itemRootElement.Add(new TextField() { name = "DescriptionField", isDelayed = true, multiline = true });

			itemRootElement.Add(new Button() { text = "-" });

			return itemRootElement;
		}

		private void BindListItem(VisualElement element, int index) {
			element.style.paddingTop = StyleKeyword.Null;
			element.style.paddingBottom = StyleKeyword.Null;

			while (index >= m_nameCallbacks.Count) {
				int callbackIndex = m_nameCallbacks.Count;

				m_nameCallbacks.Add(evt => OnNameFieldChanged(evt, callbackIndex));
				m_descriptionCallbacks.Add(evt => OnDescriptionFieldChanged(evt, callbackIndex));
				m_deleteActions.Add(() => DeleteAssetAt(callbackIndex));
			}

			GlobalObjectAsset asset = m_assetList[index];
			UQueryState<TextField> textFields = element.Query<TextField>().Build();

			textFields.AtIndex(0).SetValueWithoutNotify(asset.GUIDString);

			TextField nameField = textFields.AtIndex(1);
			nameField.RegisterValueChangedCallback(m_nameCallbacks[index]);
			nameField.SetValueWithoutNotify(asset.Name);

			TextField descriptionField = textFields.AtIndex(2);
			descriptionField.RegisterValueChangedCallback(m_descriptionCallbacks[index]);
			descriptionField.SetValueWithoutNotify(asset.Description);

			element.Q<Button>().clicked += m_deleteActions[index];
		}

		private void UnbindListItem(VisualElement element, int index) {
			UQueryState<TextField> textFields = element.Query<TextField>().Build();

			TextField nameField = textFields.AtIndex(1);
			nameField.UnregisterValueChangedCallback(m_nameCallbacks[index]);

			TextField descriptionField = textFields.AtIndex(2);
			descriptionField.UnregisterValueChangedCallback(m_descriptionCallbacks[index]);

			element.Q<Button>().clicked -= m_deleteActions[index];
		}

		private void OnNameFieldChanged(ChangeEvent<string> evt, int index) {
			GlobalObjectAsset asset = m_assetList[index];
			if (asset.Name == evt.newValue) return;

			GlobalObjectDatabase.RecordUndo("Modify global object name");
			asset.Name = evt.newValue;
			GlobalObjectDatabase.UpdateAsset(asset);
		}

		private void OnDescriptionFieldChanged(ChangeEvent<string> evt, int index) {
			GlobalObjectAsset asset = m_assetList[index];
			if (asset.Description == evt.newValue) return;

			GlobalObjectDatabase.RecordUndo("Modify global object description");
			asset.Description = evt.newValue;
			GlobalObjectDatabase.UpdateAsset(asset);
		}

		private void DeleteAssetAt(int index) {
			GlobalObjectDatabase.RecordUndo("Delete global object");
			GlobalObjectDatabase.DeleteAsset(m_assetList[index]);

			m_assetList.RemoveAt(index);
			m_listView.RefreshItems();
		}
	}
}
