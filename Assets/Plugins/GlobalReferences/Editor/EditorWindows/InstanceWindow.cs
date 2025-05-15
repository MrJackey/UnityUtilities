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

		private ListView m_listView;
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

			m_listView = new ListView {
				itemsSource = m_instances,
				makeItem = CreateListItem,
				bindItem = BindListItem,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				selectionType = SelectionType.None,
			};
			m_listView.TrackPropertyValue(GlobalObjectDatabase.AssetsProperty, OnPropertyChanged);

			rootVisualElement.Add(m_searchField = new SearchField(OnSearchValueChanged));

			VisualElement header = new VisualElement() { name = "Header" };
			header.Add(new Label("GUID") { name = "GUIDHeader" });
			header.Add(new Label("Name"));
			header.Add(new Label("Instance"));
			rootVisualElement.Add(header);
			rootVisualElement.Add(m_listView);

			GlobalReferenceManager.ListUpdated += OnListUpdated;
			GlobalObjectDatabase.AssetsLoaded += OnAssetsLoaded;

			RefreshSearch(string.Empty);
		}

		private void OnDestroy() {
			GlobalReferenceManager.ListUpdated -= OnListUpdated;
			GlobalObjectDatabase.AssetsLoaded -= OnAssetsLoaded;
		}

		private VisualElement CreateListItem() {
			VisualElement listItem = new VisualElement() { name = "ListItem" };

			TextField guidField = new TextField() { name = "GUIDField" };
			guidField.Q<TextElement>().SetEnabled(false);
			guidField.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.AppendAction("Copy", _ => EditorGUIUtility.systemCopyBuffer = guidField.value )));
			listItem.Add(guidField);

			listItem.Add(new Label() { name = "NameField" });

			ObjectField instanceField = new ObjectField() { allowSceneObjects = true, objectType = typeof(GameObject) };
			instanceField.SetEnabled(false);
			listItem.Add(instanceField);

			return listItem;
		}

		private void BindListItem(VisualElement element, int index) {
			(GlobalObjectAsset asset, GlobalId instance) = m_instances[index];
			bool missing = asset == s_missingAsset;

			TextField guidField = element.Q<TextField>();
			guidField.SetValueWithoutNotify(instance.GUID.ToString());
			guidField.EnableInClassList("Missing", missing);

			Label nameLabel = element.Q<Label>();
			nameLabel.text = asset.Name;
			nameLabel.EnableInClassList("Missing", missing);

			element.Q<ObjectField>().SetValueWithoutNotify(instance);
		}

		private void OnSearchValueChanged(ChangeEvent<string> evt) => RefreshSearch(evt.newValue);
		private void RefreshSearch(string input) {
			if (string.IsNullOrEmpty(input)) {
				m_instances.Clear();
				m_instances.AddRange(GlobalReferenceManager.InstanceList.Select(id => GlobalObjectDatabase.TryGetAsset(id.GUID, out GlobalObjectAsset asset) ? (asset, id) : (s_missingAsset, id)));

				m_listView.RefreshItems();
				return;
			}

			if (SerializedGUID.TryParse(input, out SerializedGUID guid)) {
				m_instances.Clear();
				m_instances.AddRange(GlobalReferenceManager.InstanceList
					.Where(id => id.GUID == guid)
					.Select(id => GlobalObjectDatabase.TryGetAsset(id.GUID, out GlobalObjectAsset asset) ? (asset, id) : (s_missingAsset, id))
				);

				m_listView.RefreshItems();
				return;
			}

			List<GlobalObjectAsset> instanceAssets = GlobalReferenceManager.InstanceList.Select(id => GlobalObjectDatabase.TryGetAsset(id.GUID, out GlobalObjectAsset asset) ? asset : s_missingAsset).ToList();
			Search.Execute(
				instanceAssets,
				asset => asset.Name,
				input,
				m_searchList
			);
			m_instances.Clear();
			m_instances.AddRange(m_searchList.Select(index => (instanceAssets[index], GlobalReferenceManager.InstanceList[index])));

			m_listView.RefreshItems();
		}

		private void OnAssetsLoaded() => RefreshSearch(m_searchField.TextField.value);
		private void OnListUpdated() => RefreshSearch(m_searchField.TextField.value);
		private void OnPropertyChanged(SerializedProperty _) => RefreshSearch(m_searchField.TextField.value);
	}
}
