using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Editor.ObjectPicker;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(GlobalRef))]
	public class GlobalRefPropertyDrawer : PropertyDrawer {
		private static readonly StyleSheet s_searchStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("04bac9d34ae64c4ba26fd78ee16e4031"));
		private static readonly StyleSheet s_refStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("033b4153331442e39690a4ab85195160"));

		private static readonly Texture s_searchIcon = EditorGUIUtility.IconContent("d_Search Icon").image;
		private static readonly Texture s_fieldsIcon = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;

		private SerializedProperty m_guidProperty;
		private GlobalObjectAsset m_asset;

		private VisualElement m_assetFieldsRoot;
		private VisualElement m_searchRoot;

		private Foldout m_foldOut;
		private Image m_fieldsImage;
		private Image m_dropOverlay;

		private TextField m_guidField;
		private TextField m_nameField;
		private TextField m_descriptionField;

		private ListView m_searchListView;
		private List<GlobalObjectAsset> m_searchList;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();
			rootVisualElement.styleSheets.Add(s_searchStyleSheet);
			rootVisualElement.styleSheets.Add(s_refStyleSheet);

			rootVisualElement.RegisterCallback<AttachToPanelEvent, GlobalRefPropertyDrawer>((evt, self) => {
				GlobalObjectDatabase.AssetsLoaded += self.OnAssetsLoaded;
				GlobalReferenceManager.ListUpdated += self.RefreshInstanceCheck;
			}, this);
			rootVisualElement.RegisterCallback<DetachFromPanelEvent, GlobalRefPropertyDrawer>((evt, self) => {
				GlobalObjectDatabase.AssetsLoaded -= self.OnAssetsLoaded;
				GlobalReferenceManager.ListUpdated -= self.RefreshInstanceCheck;
			}, this);

			m_guidProperty = property.FindPropertyRelative("m_guid");
			SerializedGUID guid = SerializedGUID.Editor_GetFromProperty(m_guidProperty);

			bool hasAssignedAsset = GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset asset);

			m_foldOut = new Foldout() {
				value = false,
				text = property.displayName,
			};
			m_foldOut.RegisterCallback<DragUpdatedEvent, GlobalRefPropertyDrawer>(OnDragUpdated, this);
			m_foldOut.RegisterCallback<DragPerformEvent, GlobalRefPropertyDrawer>(OnDragPerformed, this);
			rootVisualElement.Add(m_foldOut);

			VisualElement toggleHeaderContent = m_foldOut.Q<Toggle>()[0];
			toggleHeaderContent.Q<Label>().name = "PropertyDisplayName";

			TextField assetNameField = new TextField() { value = hasAssignedAsset ? asset.Name : string.Empty, name = "AssetName" };
			assetNameField[0].SetEnabled(false);
			assetNameField.AddManipulator(new Clickable(() => {
				if (m_asset != null && GlobalReferenceManager.TryResolve(m_asset.GUID, out GameObject go)) {
					EditorGUIUtility.PingObject(go);
				}
			}));
			toggleHeaderContent.Add(assetNameField);

			Button fieldsButton = new Button(OnModeButtonClicked) { name = "FieldsButton" };
			m_fieldsImage = new Image() { image = hasAssignedAsset ? s_searchIcon : s_fieldsIcon };
			fieldsButton.Add(m_fieldsImage);
			m_foldOut.hierarchy.Add(fieldsButton);

			Button clearButton = new Button(ClearReference) { name = "ClearButton", text = "-" };
			m_foldOut.hierarchy.Add(clearButton);

			m_assetFieldsRoot = new VisualElement() {
				style = { display = hasAssignedAsset ? DisplayStyle.Flex : DisplayStyle.None },
			};
			m_assetFieldsRoot.TrackPropertyValue(GlobalObjectDatabase.AssetsProperty, OnPropertyChanged);
			m_guidField = new TextField("GUID") { name = "GUIDField" };
			m_guidField.SetEnabled(false);
			m_guidField.TrackPropertyValue(m_guidProperty, OnPropertyChanged);
			m_assetFieldsRoot.Add(m_guidField);

			m_nameField = new TextField("Name") { name = "NameField", isDelayed = true };
			m_nameField.RegisterValueChangedCallback(OnNameChanged);
			m_assetFieldsRoot.Add(m_nameField);

			m_descriptionField = new TextField("Description") { name = "DescriptionField", isDelayed = true, multiline = true };
			m_descriptionField.RegisterValueChangedCallback(OnDescriptionChanged);
			m_assetFieldsRoot.Add(m_descriptionField);

			m_foldOut.Add(m_assetFieldsRoot);

			if (hasAssignedAsset)
				SetAssetFields(asset);

			m_searchRoot = new VisualElement() {
				style = { display = hasAssignedAsset ? DisplayStyle.None : DisplayStyle.Flex },
			};

			m_searchList = new List<GlobalObjectAsset>();
			m_searchListView = new ListView() {
				makeItem = CreateListItem,
				bindItem = BindListItem,
				selectionType = SelectionType.Single,
				showBorder = true,
				reorderable = false,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
			};
			m_searchListView.selectionChanged += _ => OnSearchSelectionChanged();

			m_searchRoot.Add(new GlobalObjectPicker(m_searchListView, m_searchList));
			m_searchRoot.Add(m_searchListView);

			m_foldOut.Add(m_searchRoot);

			m_searchListView.itemsSource = m_searchList;

			return rootVisualElement;
		}

		private VisualElement CreateListItem() {
			VisualElement listItemRoot = new VisualElement() { name = "ListItem" };

			TextField guidField = new TextField() { name = "GUIDField" };
			guidField.SetEnabled(false);
			listItemRoot.Add(guidField);

			listItemRoot.Add(new Label() { name = "Name" });

			return listItemRoot;
		}

		private void BindListItem(VisualElement element, int index) {
			element.Q<TextField>().SetValueWithoutNotify(m_searchList[index].GUIDString);
			element.Q<Label>().text = m_searchList[index].Name;
		}

		private void OnModeButtonClicked() {
			m_foldOut.value = true;

			SerializedGUID guid = SerializedGUID.Editor_GetFromProperty(m_guidProperty);
			bool assignedAsset = GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset asset);

			bool toSearch = !assignedAsset || m_searchRoot.resolvedStyle.display == DisplayStyle.None;

			ToggleFields(toSearch);
		}

		private void ToggleFields(bool toSearch) {
			m_assetFieldsRoot.style.display = toSearch ? DisplayStyle.None : DisplayStyle.Flex;
			m_searchRoot.style.display = toSearch ? DisplayStyle.Flex : DisplayStyle.None;
			m_fieldsImage.image = toSearch ? s_fieldsIcon : s_searchIcon;
			m_searchListView.SetSelection(-1);
		}

		private void ClearReference() {
			SerializedGUID.Editor_WriteToProperty(m_guidProperty, default);
			m_guidProperty.serializedObject.ApplyModifiedProperties();

			m_foldOut.Q<TextField>().SetValueWithoutNotify(string.Empty);
			ToggleFields(true);

			m_asset = null;
		}

		private void OnSearchSelectionChanged() {
			if (m_searchListView.selectedIndex == -1) return;

			GlobalObjectAsset asset = m_searchList[m_searchListView.selectedIndex];
			SerializedGUID.Editor_WriteToProperty(m_guidProperty, asset.GUID);
			m_guidProperty.serializedObject.ApplyModifiedProperties();

			SetAssetFields(asset);
			ToggleFields(false);
		}

		private void SetAssetFields(GlobalObjectAsset asset) {
			m_asset = asset;

			m_foldOut.Q<TextField>().SetValueWithoutNotify(asset.Name);

			m_guidField.SetValueWithoutNotify(asset.GUIDString);
			m_nameField.SetValueWithoutNotify(asset.Name);
			m_descriptionField.SetValueWithoutNotify(asset.Description);

			RefreshInstanceCheck();
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

		private void OnDragUpdated(DragUpdatedEvent evt, GlobalRefPropertyDrawer self) {
			if (!self.TryGetDraggedId(out _)) return;

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
		}

		private void OnDragPerformed(DragPerformEvent evt, GlobalRefPropertyDrawer self) {
			if (!self.TryGetDraggedId(out GlobalId draggedId)) return;

			SerializedGUID.Editor_WriteToProperty(self.m_guidProperty, draggedId.GUID);
			self.m_guidProperty.serializedObject.ApplyModifiedProperties();

			bool result = GlobalObjectDatabase.TryGetAsset(draggedId.GUID, out GlobalObjectAsset draggedAsset);
			Debug.Assert(result);

			self.SetAssetFields(draggedAsset);
			self.ToggleFields(false);
		}

		private bool TryGetDraggedId(out GlobalId id) {
			foreach (Object obj in DragAndDrop.objectReferences) {
				if (obj is GlobalId compId && GlobalObjectDatabase.TryGetAsset(compId.GUID, out _)) {
					id = compId;
					return true;
				}

				if (obj is GameObject go && go.TryGetComponent(out id) && GlobalObjectDatabase.TryGetAsset(id.GUID, out _))
					return true;
			}

			id = null;
			return false;
		}

		private void RefreshInstanceCheck() {
			if (m_asset != null && GlobalReferenceManager.TryResolve(m_asset.GUID, out _))
				m_foldOut.Q<TextField>().AddToClassList("Present");
			else
				m_foldOut.Q<TextField>().RemoveFromClassList("Present");
		}

		private void OnAssetsLoaded() => OnPropertyChanged(null);
		private void OnPropertyChanged(SerializedProperty _) {
			SerializedGUID guid = SerializedGUID.Editor_GetFromProperty(m_guidProperty);
			if (GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset asset)) {
				SetAssetFields(asset);
			}
			else {
				ToggleFields(true);
				m_foldOut.Q<TextField>().SetValueWithoutNotify(string.Empty);
			}
		}
	}
}
