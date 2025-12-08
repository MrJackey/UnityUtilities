using UnityEditor;
using UnityEngine.UIElements;

namespace Jackey.GlobalReferences.Editor.ObjectPicker {
	public class SearchField : VisualElement {
		private static readonly StyleSheet s_styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("9c4f1630f36348b8a3986ecaeda73da3"));

		private TextField m_textField;
		private Image m_clearSearchImage;

		public TextField TextField => m_textField;

		public SearchField(EventCallback<ChangeEvent<string>> searchAction) {
			styleSheets.Add(s_styleSheet);

			m_textField = new TextField();
			m_textField.RegisterValueChangedCallback(OnTextFieldValueChanged);
			m_textField.RegisterValueChangedCallback(searchAction);
			Add(m_textField);

			Add(new Image() {
				name = "SearchIcon",
				image = EditorGUIUtility.IconContent("d_Search Icon").image,
				pickingMode = PickingMode.Ignore,
			});

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
		}

		private void OnTextFieldValueChanged(ChangeEvent<string> evt) {
			m_clearSearchImage.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
		}
	}
}
