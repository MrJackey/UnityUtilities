using System.Collections.Generic;
using System.Linq;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Editor.ObjectPicker;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.EditorWindows {
	public class InstanceWindow : EditorWindow {
		[SerializeField] private StyleSheet m_styleSheet;

		private static readonly GlobalObjectAsset s_missingAsset = new() { Name = "MISSING ASSET" };

		private SearchField m_searchField;

		private MultiColumnListView m_listView;
		private List<int> m_searchList = new();

		private List<(GlobalObjectAsset asset, GlobalId instance)> m_instances = new();

		[MenuItem("Tools/Jackey/Global References/Instances", priority = 1000)]
		private static void ShowWindow() {
			InstanceWindow window = GetWindow<InstanceWindow>();
			window.Show();
		}

		private void CreateGUI() {
			titleContent = new GUIContent("Global Object Instances");

			rootVisualElement.styleSheets.Add(m_styleSheet);

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
						title = "Name",
						stretchable = true,
						// sortable = true,
					},
					new Column() {
						makeCell = CreateInstanceCell,
						bindCell = BindInstanceCell,
						title = "Instance",
						stretchable = true,
						resizable = false,
						// sortable = true,
					},
				},
				itemsSource = m_instances,
				selectionType = SelectionType.None,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				showBoundCollectionSize = true,
				// sortingEnabled = true,
			};

			m_listView.TrackPropertyValue(GlobalObjectDatabase.AssetsProperty, OnPropertyChanged);

			rootVisualElement.Add(m_searchField = new SearchField(OnSearchValueChanged));
			rootVisualElement.Add(m_listView);

			GlobalReferenceManager.EditModeListUpdated += OnEditModeListUpdated;
			GlobalObjectDatabase.AssetsLoaded += OnAssetsLoaded;

			RefreshSearch(string.Empty);
		}

		private void OnDestroy() {
			GlobalReferenceManager.EditModeListUpdated -= OnEditModeListUpdated;
			GlobalObjectDatabase.AssetsLoaded -= OnAssetsLoaded;
		}

		private VisualElement CreateGUIDCell() {
			TextField field = new TextField() { name = "GUIDField" };
			field.Q<TextElement>().SetEnabled(false);
			field.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.AppendAction("Copy", _ => EditorGUIUtility.systemCopyBuffer = field.value )));

			return field;
		}

		private void BindGUIDCell(VisualElement element, int index) {
			(GlobalObjectAsset asset, GlobalId instance) = m_instances[index];

			TextField field = (TextField)element;
			field.SetValueWithoutNotify(instance.GUID.ToString());
			field.EnableInClassList("Missing", asset == s_missingAsset);
		}

		private VisualElement CreateNameCell() {
			return new Label() { name = "NameLabel" };
		}

		private void BindNameCell(VisualElement element, int index) {
			(GlobalObjectAsset asset, GlobalId _) = m_instances[index];

			Label label = (Label)element;
			label.text = asset.Name;
			label.EnableInClassList("Missing", asset == s_missingAsset);
		}

		private VisualElement CreateInstanceCell() {
			ObjectField field = new ObjectField() { allowSceneObjects = true, objectType = typeof(GameObject) };
			field.SetEnabled(false);

			return field;
		}

		private void BindInstanceCell(VisualElement element, int index) {
			(GlobalObjectAsset _, GlobalId instance) = m_instances[index];
			((ObjectField)element).SetValueWithoutNotify(instance);
		}

		private void OnSearchValueChanged(ChangeEvent<string> evt) => RefreshSearch(evt.newValue);
		private void RefreshSearch(string input) {
			if (string.IsNullOrEmpty(input)) {
				m_instances.Clear();
				m_instances.AddRange(GlobalReferenceManager.EditMode_InstanceList.Select(id => GlobalObjectDatabase.TryGetAsset(id.GUID, out GlobalObjectAsset asset) ? (asset, id) : (s_missingAsset, id)));

				m_listView.RefreshItems();
				return;
			}

			if (SerializedGUID.TryParse(input, out SerializedGUID guid)) {
				m_instances.Clear();
				m_instances.AddRange(GlobalReferenceManager.EditMode_InstanceList
					.Where(id => id.GUID == guid)
					.Select(id => GlobalObjectDatabase.TryGetAsset(id.GUID, out GlobalObjectAsset asset) ? (asset, id) : (s_missingAsset, id))
				);

				m_listView.RefreshItems();
				return;
			}

			List<GlobalObjectAsset> instanceAssets = GlobalReferenceManager.EditMode_InstanceList.Select(id => GlobalObjectDatabase.TryGetAsset(id.GUID, out GlobalObjectAsset asset) ? asset : s_missingAsset).ToList();
			Search.Execute(
				instanceAssets,
				asset => asset.Name,
				input,
				m_searchList
			);
			m_instances.Clear();
			m_instances.AddRange(m_searchList.Select(index => (instanceAssets[index], GlobalReferenceManager.EditMode_InstanceList[index])));

			m_listView.RefreshItems();
		}

		private void OnAssetsLoaded() => RefreshSearch(m_searchField.TextField.value);
		private void OnEditModeListUpdated() => RefreshSearch(m_searchField.TextField.value);
		private void OnPropertyChanged(SerializedProperty _) => RefreshSearch(m_searchField.TextField.value);
	}
}
