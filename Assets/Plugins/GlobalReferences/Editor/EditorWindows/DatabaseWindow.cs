using System;
using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Editor.ObjectPicker;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.EditorWindows {
	public class DatabaseWindow : EditorWindow {
		[SerializeField] private StyleSheet m_styleSheet;

		private List<GlobalObjectAsset> m_assetList;
		private MultiColumnListView m_listView;

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

			rootVisualElement.styleSheets.Add(m_styleSheet);

			m_assetList = new List<GlobalObjectAsset>();

			m_listView = new MultiColumnListView() {
				columns = {
					new Column() {
						makeCell = CreateGUIDCell,
						bindCell = BindGUIDCell,
						title = "GUID",
						width = 275f,
						resizable = false,
						sortable = false,
					},
					new Column() {
						makeCell = CreateNameCell,
						bindCell = BindNameCell,
						unbindCell = UnbindNameCell,
						title = "Name",
						stretchable = true,
						// sortable = true,
					},
					new Column() {
						makeCell = CreateDescriptionCell,
						bindCell = BindDescriptionCell,
						unbindCell = UnbindDescriptionCell,
						title = "Description",
						stretchable = true,
						resizable = false,
						// sortable = true,
					},
					new Column() {
						makeCell = CreateDeleteCell,
						bindCell = BindDeleteCell,
						unbindCell = UnbindDeleteCell,
						resizable = false,
						stretchable = false,
						sortable = false,
					},
				},
				selectionType = SelectionType.None,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				showBoundCollectionSize = true,
				// sortingEnabled = true,
			};

			rootVisualElement.Add(new GlobalObjectPicker(m_listView, m_assetList));

			m_listView.itemsSource = m_assetList;
			rootVisualElement.Add(m_listView);
		}

		private VisualElement CreateGUIDCell() {
			TextField field = new TextField() { name = "GUIDField" };
			field.Q<TextElement>().SetEnabled(false);
			field.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.AppendAction("Copy", _ => EditorGUIUtility.systemCopyBuffer = field.value )));

			return field;
		}

		private void BindGUIDCell(VisualElement element, int index) {
			((TextField)element).SetValueWithoutNotify(m_assetList[index].GUIDString);
		}

		private VisualElement CreateNameCell() {
			return new TextField() { isDelayed = true };
		}

		private void BindNameCell(VisualElement element, int index) {
			while (index >= m_nameCallbacks.Count) {
				int callbackIndex = m_nameCallbacks.Count;
				m_nameCallbacks.Add(evt => OnNameFieldChanged(evt, callbackIndex));
			}

			TextField field = (TextField)element;
			field.RegisterValueChangedCallback(m_nameCallbacks[index]);
			field.SetValueWithoutNotify(m_assetList[index].Name);
		}

		private void UnbindNameCell(VisualElement element, int index) {
			((TextField)element).UnregisterValueChangedCallback(m_nameCallbacks[index]);
		}

		private VisualElement CreateDescriptionCell() {
			return new TextField() { name = "DescriptionField", isDelayed = true, multiline = true };
		}

		private void BindDescriptionCell(VisualElement element, int index) {
			while (index >= m_descriptionCallbacks.Count) {
				int callbackIndex = m_descriptionCallbacks.Count;
				m_descriptionCallbacks.Add(evt => OnDescriptionFieldChanged(evt, callbackIndex));
			}

			TextField field = (TextField)element;
			field.RegisterValueChangedCallback(m_descriptionCallbacks[index]);
			field.SetValueWithoutNotify(m_assetList[index].Description);
		}

		private void UnbindDescriptionCell(VisualElement element, int index) {
			((TextField)element).UnregisterValueChangedCallback(m_descriptionCallbacks[index]);
		}

		private VisualElement CreateDeleteCell() {
			return new Button() { name = "DeleteButton", text = "-" };
		}

		private void BindDeleteCell(VisualElement element, int index) {
			while (index >= m_deleteActions.Count) {
				int callbackIndex = m_deleteActions.Count;
				m_deleteActions.Add(() => DeleteAssetAt(callbackIndex));
			}

			((Button)element).clicked += m_deleteActions[index];
		}

		private void UnbindDeleteCell(VisualElement element, int index) {
			((Button)element).clicked -= m_deleteActions[index];
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
