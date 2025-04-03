// using System;
// using Jackey.Behaviours.BehaviourTree.Composites;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace Jackey.Behaviours.Editor.TypeSearch {
// 	public class TypeSearchWindow : EditorWindow {
// 		private const float DEFAULT_WIDTH = 240f;
// 		private const float DEFAULT_HEIGHT = 320f;
//
// 		private static TypeSearchWindow s_window;
// 		private EditorWindow m_wasFocusedWindow;
//
// 		private ToolbarSearchField m_searchField;
// 		private Label m_header;
// 		private ListView m_listView;
//
// 		private Action<Type> m_callback;
//
// 		public static void AskForType(TypeCache.TypeCollection types, Action<Type> callback) {
// 			Debug.Log("Asking for type");
//
// 			EditorWindow wasFocusedWindow = focusedWindow;
// 			Debug.Log(wasFocusedWindow);
//
// 			TypeSearchWindow window = CreateInstance<TypeSearchWindow>();
// 			window.hideFlags = HideFlags.HideAndDontSave;
// 			window.wantsMouseMove = true;
//
// 			window.m_wasFocusedWindow = wasFocusedWindow;
// 			window.m_callback = callback;
//
// 			Vector2 position = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
// 			position.x -= DEFAULT_WIDTH / 2f;
// 			Vector2 size = new Vector2(DEFAULT_WIDTH, DEFAULT_HEIGHT);
// 			window.ShowAsDropDown(new Rect(position, Vector2.zero), size);
// 			window.Focus();
//
// 			callback?.Invoke(typeof(Sequencer));
// 		}
//
// 		private void CreateGUI() {
// 			Toolbar toolbar = new Toolbar();
// 			toolbar.Add(m_searchField = new ToolbarSearchField());
//
// 			rootVisualElement.Add(toolbar);
// 		}
//
// 		private void OnLostFocus() {
// 			if (hasFocus)
// 				Close();
// 		}
//
// 		private void OnDestroy() {
// 			if (m_wasFocusedWindow)
// 				m_wasFocusedWindow.Focus();
// 		}
// 	}
// }
