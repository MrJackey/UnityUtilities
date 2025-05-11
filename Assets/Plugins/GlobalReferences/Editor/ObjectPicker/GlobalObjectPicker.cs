using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.ObjectPicker {
	public class GlobalObjectPicker : VisualElement {
		TextField m_textField;
		private Image m_clearSearchImage;

		private ListView m_listView;
		private List<GlobalObjectAsset> m_list;

		public GlobalObjectPicker(ListView listView, List<GlobalObjectAsset> list) {
			m_listView = listView;
			m_list = list;

			Add(new Image() { name = "SearchIcon", image = EditorGUIUtility.IconContent("d_Search Icon").image });

			m_textField = new TextField();
			m_textField.RegisterValueChangedCallback(OnSearchValueChanged);
			Add(m_textField);

			m_clearSearchImage = new Image() {
				name = "ClearSearch",
				image = EditorGUIUtility.IconContent("d_winbtn_win_close").image,
				style = { display = DisplayStyle.None, },
			};
			m_clearSearchImage.AddManipulator(new Clickable(() => {
				m_textField.value = string.Empty;
				m_textField.Blur();
			}));
			m_textField.Add(m_clearSearchImage);

			Button newButton = new Button(CreateNewAsset);
			newButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Toolbar Plus").image });
			Add(newButton);

			m_list.Clear();
			foreach (GlobalObjectAsset asset in GlobalObjectDatabase.instance.Assets)
				m_list.Add(asset);

			this.TrackPropertyValue(GlobalObjectDatabase.AssetsProperty, OnPropertyChanged);
			RegisterCallback<AttachToPanelEvent, GlobalObjectPicker>((evt, picker) => GlobalObjectDatabase.AssetsLoaded += picker.OnAssetsLoaded, this);
			RegisterCallback<DetachFromPanelEvent, GlobalObjectPicker>((evt, picker) => GlobalObjectDatabase.AssetsLoaded -= picker.OnAssetsLoaded, this);
		}

		private void OnSearchValueChanged(ChangeEvent<string> evt) {
			SearchWithInput(evt.newValue);
		}

		private void SearchWithInput(string input) {
			m_listView.selectedIndex = -1;

			if (string.IsNullOrEmpty(input)) {
				m_clearSearchImage.style.display = DisplayStyle.None;

				m_list.Clear();
				foreach (GlobalObjectAsset asset in GlobalObjectDatabase.instance.Assets)
					m_list.Add(asset);

				m_listView.RefreshItems();
				return;
			}

			m_clearSearchImage.style.display = DisplayStyle.Flex;

			if (SerializedGUID.TryParse(input, out SerializedGUID guid)) {
				m_list.Clear();

				if (GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset guidAsset))
					m_list.Add(guidAsset);

				m_listView.RefreshItems();
				return;
			}

			Search.Execute(GlobalObjectDatabase.instance.Assets, m_list, input);

			m_listView.RefreshItems();
		}

		private void CreateNewAsset() {
			GlobalObjectAsset asset = GlobalObjectDatabase.CreateAsset();

			m_list.Add(asset);
			m_textField.value = string.Empty;
			m_listView.SetSelection(m_list.Count - 1);
		}

		private void OnPropertyChanged(SerializedProperty _) {
			SearchWithInput(m_textField.value);
		}

		private void OnAssetsLoaded() {
			SearchWithInput(m_textField.value);
		}
	}
}
