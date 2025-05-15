using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.ObjectPicker {
	public class GlobalObjectPicker : VisualElement {
		private static StyleSheet s_styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("04bac9d34ae64c4ba26fd78ee16e4031"));

		private SearchField m_searchField;

		private ListView m_listView;
		private List<GlobalObjectAsset> m_output;

		public GlobalObjectPicker(ListView listView, List<GlobalObjectAsset> output) {
			styleSheets.Add(s_styleSheet);

			m_listView = listView;
			m_output = output;

			Add(m_searchField = new SearchField(OnSearchValueChanged));

			Button newButton = new Button(CreateNewAsset);
			newButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Toolbar Plus").image });
			Add(newButton);

			m_output.Clear();
			foreach (GlobalObjectAsset asset in GlobalObjectDatabase.instance.Assets)
				m_output.Add(asset);

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
				m_output.Clear();
				foreach (GlobalObjectAsset asset in GlobalObjectDatabase.instance.Assets)
					m_output.Add(asset);

				m_listView.RefreshItems();
				return;
			}

			if (SerializedGUID.TryParse(input, out SerializedGUID guid)) {
				m_output.Clear();

				if (GlobalObjectDatabase.TryGetAsset(guid, out GlobalObjectAsset guidAsset))
					m_output.Add(guidAsset);

				m_listView.RefreshItems();
				return;
			}

			Search.Execute(GlobalObjectDatabase.instance.Assets, m_output, input);

			m_listView.RefreshItems();
		}

		private void CreateNewAsset() {
			GlobalObjectAsset asset = GlobalObjectDatabase.CreateAsset();

			m_output.Add(asset);
			m_searchField.TextField.value = string.Empty;
			m_listView.SetSelection(m_output.Count - 1);
		}

		private void OnPropertyChanged(SerializedProperty _) {
			SearchWithInput(m_searchField.TextField.value);
		}

		private void OnAssetsLoaded() {
			SearchWithInput(m_searchField.TextField.value);
		}
	}
}
