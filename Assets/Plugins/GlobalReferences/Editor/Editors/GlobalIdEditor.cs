using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Editor.ObjectPicker;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.Editors {
	[CustomEditor(typeof(GlobalId))]
	public class GlobalIdEditor : UnityEditor.Editor {
		[SerializeField] private StyleSheet m_searchStyleSheet;
		[SerializeField] private StyleSheet m_listStyleSheet;

		private List<GlobalObjectAsset> m_searchList = new();
		private ListView m_searchListView;

		private SerializedProperty m_guidProperty;
		private GlobalObjectAsset m_asset;

		private VisualElement m_header;

		private VisualElement m_assetFieldsRoot;
		private TextField m_guidField;
		private TextField m_nameField;
		private TextField m_descriptionField;

		private VisualElement m_searchRoot;

		public override VisualElement CreateInspectorGUI() {
			VisualElement rootVisualElement = new VisualElement();
			rootVisualElement.styleSheets.Add(m_searchStyleSheet);
			rootVisualElement.styleSheets.Add(m_listStyleSheet);

			rootVisualElement.SetEnabled(!Application.IsPlaying(target));

			rootVisualElement.TrackPropertyValue(GlobalObjectDatabase.AssetsProperty, OnPropertyChanged);
			GlobalObjectDatabase.AssetsLoaded += OnAssetsLoaded;

			m_guidProperty = serializedObject.FindProperty("m_guid");
			SerializedGUID guid = SerializedGUID.Editor_GetFromProperty(m_guidProperty);
			bool hasAssignedAsset = GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset asset);

			m_header = new VisualElement() { name = "Header", style = { display = hasAssignedAsset ? DisplayStyle.Flex : DisplayStyle.None }};

			Button fieldsButton = new Button(ShowFields);
			fieldsButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image });
			m_header.Add(fieldsButton);
			Button searchButton = new Button(ShowSearch);
			searchButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Search Icon").image });
			m_header.Add(searchButton);

			rootVisualElement.Add(m_header);

			// Asset
			m_assetFieldsRoot = new VisualElement() { name = "AssetFields", style = { display = hasAssignedAsset ? DisplayStyle.Flex : DisplayStyle.None }};

			m_assetFieldsRoot.Add(m_guidField = new TextField("GUID"));
			m_guidField.SetEnabled(false);

			m_assetFieldsRoot.Add(m_nameField = new TextField("Name") { isDelayed = true });
			m_nameField.RegisterValueChangedCallback(OnNameChanged);

			m_assetFieldsRoot.Add(m_descriptionField = new TextField("Description") { isDelayed = true, multiline = true });
			m_descriptionField.RegisterValueChangedCallback(OnDescriptionChanged);

			m_assetFieldsRoot.TrackPropertyValue(m_guidProperty, OnPropertyChanged);

			if (hasAssignedAsset)
				SetAssetFields(asset);

			rootVisualElement.Add(m_assetFieldsRoot);

			// Search
			m_searchRoot = new VisualElement() { style = { display = hasAssignedAsset ? DisplayStyle.None : DisplayStyle.Flex }};

			m_searchListView = new ListView {
				makeItem = CreateListItem,
				bindItem = BindListItem,
				reorderable = false,
				showAddRemoveFooter = false,
				showBoundCollectionSize = true,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				selectionType = SelectionType.Single,
				showBorder = true,
			};
			m_searchListView.selectedIndicesChanged += _ => OnSearchSelectionChanged();

			m_searchRoot.Add(new GlobalObjectPicker(m_searchListView, m_searchList));
			m_searchRoot.Add(m_searchListView);

			rootVisualElement.Add(m_searchRoot);

			m_searchListView.itemsSource = m_searchList;

			return rootVisualElement;
		}

		private void OnDestroy() {
			GlobalObjectDatabase.AssetsLoaded -= OnAssetsLoaded;
		}

		private void ShowFields() {
			if (m_asset == null) return;

			m_searchRoot.style.display = DisplayStyle.None;
			m_assetFieldsRoot.style.display = DisplayStyle.Flex;
		}

		private void ShowSearch() {
			m_searchRoot.style.display = DisplayStyle.Flex;
			m_assetFieldsRoot.style.display = DisplayStyle.None;
			m_searchListView.SetSelection(-1);
		}

		private VisualElement CreateListItem() {
			VisualElement rootListItem = new VisualElement() { name = "ListItem" };

			TextField guidField = new TextField() { name = "GUIDField"};
			guidField.SetEnabled(false);
			rootListItem.Add(guidField);

			rootListItem.Add(new Label() { name = "Name" });

			return rootListItem;
		}

		private void BindListItem(VisualElement element, int index) {
			element.Q<TextField>().SetValueWithoutNotify(m_searchList[index].GUIDString);
			element.Q<Label>().text = m_searchList[index].Name;
		}

		private void OnSearchSelectionChanged() {
			if (m_searchListView.selectedIndex == -1) return;

			GlobalObjectAsset selectedAsset = m_searchList[m_searchListView.selectedIndex];
			SerializedGUID selectedGuid = selectedAsset.GUID;

			SerializedGUID.Editor_WriteToProperty(m_guidProperty, selectedGuid);
			serializedObject.ApplyModifiedProperties();

			GlobalObjectDatabase.TryGetAsset(selectedGuid, out GlobalObjectAsset asset);
			SetAssetFields(asset);
			ShowFields();
			m_header.style.display = DisplayStyle.Flex;
		}

		private void OnAssetsLoaded() => OnPropertyChanged(null);
		private void OnPropertyChanged(SerializedProperty _) {
			SerializedGUID guid = SerializedGUID.Editor_GetFromProperty(m_guidProperty);
			bool hasAssignedAsset = GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset asset);
			m_header.style.display = hasAssignedAsset ? DisplayStyle.Flex : DisplayStyle.None;

			if (hasAssignedAsset)
				SetAssetFields(asset);
			else
				ShowSearch();
		}

		private void SetAssetFields(GlobalObjectAsset asset) {
			m_guidField.SetValueWithoutNotify(asset.GUIDString);
			m_nameField.SetValueWithoutNotify(asset.Name);
			m_descriptionField.SetValueWithoutNotify(asset.Description);

			if (PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(target)) {
				GlobalId source = (GlobalId)PrefabUtility.GetCorrespondingObjectFromSource(target);
				bool isOverride = source != null && source.GUID != asset.GUID;

				if (isOverride)
					m_assetFieldsRoot.AddToClassList("Override");
				else
					m_assetFieldsRoot.RemoveFromClassList("Override");
			}

			m_asset = asset;
		}

		private void OnNameChanged(ChangeEvent<string> evt) {
			if (evt.newValue == m_asset.Name) return;

			GlobalObjectDatabase.RecordUndo("Modify global object name");
			m_asset.Name = evt.newValue;
			GlobalObjectDatabase.UpdateAsset(m_asset);
		}

		private void OnDescriptionChanged(ChangeEvent<string> evt) {
			if (evt.newValue == m_asset.Description) return;

			GlobalObjectDatabase.RecordUndo("Modify global object description");
			m_asset.Description = evt.newValue;
			GlobalObjectDatabase.UpdateAsset(m_asset);
		}
	}
}
