using System;
using Jackey.GlobalReferences.Editor.Database;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.EditorWindows {
	public class InstanceWindow : EditorWindow {
		[SerializeField] private StyleSheet m_styleSheet;

		private ListView m_listView;

		[MenuItem("Tools/Jackey/Global References/Instances", priority = 1000)]
		private static void ShowWindow() {
			InstanceWindow window = GetWindow<InstanceWindow>();
			window.Show();
		}

		private void CreateGUI() {
			titleContent = new GUIContent("Global Object Instances");

			rootVisualElement.styleSheets.Add(m_styleSheet);

			VisualElement header = new VisualElement() { name = "Header" };
			header.Add(new Label("GUID") { name = "GUIDHeader" });
			header.Add(new Label("Name"));
			header.Add(new Label("Instance"));
			rootVisualElement.Add(header);

			m_listView = new ListView {
				itemsSource = GlobalReferenceManager.InstanceList,
				makeItem = CreateListItem,
				bindItem = BindListItem,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				selectionType = SelectionType.None,
			};
			m_listView.TrackPropertyValue(GlobalObjectDatabase.AssetsProperty, OnPropertyChanged);

			rootVisualElement.Add(m_listView);

			GlobalReferenceManager.ListUpdated += OnListUpdated;
			GlobalObjectDatabase.AssetsLoaded += OnAssetsLoaded;
		}

		private void OnDestroy() {
			GlobalReferenceManager.ListUpdated -= OnListUpdated;
			GlobalObjectDatabase.AssetsLoaded -= OnAssetsLoaded;
		}

		private VisualElement CreateListItem() {
			VisualElement listItem = new VisualElement() { name = "ListItem" };

			TextField guidField = new TextField() { name = "GUIDField" };
			guidField.SetEnabled(false);
			listItem.Add(guidField);

			listItem.Add(new Label() { name = "NameField" });

			ObjectField instanceField = new ObjectField() { allowSceneObjects = true, objectType = typeof(GameObject) };
			instanceField.SetEnabled(false);
			listItem.Add(instanceField);

			return listItem;
		}

		private void BindListItem(VisualElement element, int index) {
			GlobalId instance = GlobalReferenceManager.InstanceList[index];

			element.Q<TextField>().SetValueWithoutNotify(instance.GUID.ToString());
			element.Q<Label>().text = GlobalObjectDatabase.TryGetAsset(instance.GUID, out GlobalObjectAsset asset) ? asset.Name : string.Empty;
			element.Q<ObjectField>().SetValueWithoutNotify(instance);
		}

		private void OnAssetsLoaded() => m_listView.RefreshItems();
		private void OnListUpdated() => m_listView.RefreshItems();
		private void OnPropertyChanged(SerializedProperty _) => m_listView.RefreshItems();
	}
}
