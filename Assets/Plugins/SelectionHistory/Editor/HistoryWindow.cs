using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;
using Label = UnityEngine.UIElements.Label;
using ListView = UnityEngine.UIElements.ListView;
using Object = UnityEngine.Object;

namespace Jackey.SelectionHistory.Editor {
	public class HistoryWindow : EditorWindow {
		private List<Object> m_historyList = new();
		private ListView m_historyListView;

		private VisualElement m_controlsElement;

		[MenuItem("Tools/Jackey/Selection History/View History", false,1003)]
		private static void ShowWindow() {
			HistoryWindow window = GetWindow<HistoryWindow>();
			window.Show();
		}

		private void CreateGUI() {
			titleContent = new GUIContent("Selection History", EditorGUIUtility.IconContent("d_Grid.Default").image);

			rootVisualElement.Add(CreateHistoryListGUI());
			rootVisualElement.Add(CreateControlsGUI());

			SelectionManager.HistoryChanged += OnHistoryChanged;
			SelectionManager.MovedInHistory += OnMovedInHistory;

			EditorApplication.delayCall += OnHistoryChanged;
		}

		private void OnDisable() {
			SelectionManager.HistoryChanged -= OnHistoryChanged;
			SelectionManager.MovedInHistory -= OnMovedInHistory;

			m_historyList.Clear();
		}

		private VisualElement CreateHistoryListGUI() {
			m_historyListView = new ListView() {
				itemsSource = m_historyList,
				makeItem = MakeListItem,
				bindItem = BindListItem,
				reorderable = false,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				selectionType = SelectionType.None,
				style = {
					flexGrow = 1f,
					visibility = Visibility.Hidden,
				},
			};

			return m_historyListView;
		}

		private VisualElement MakeListItem() {
			VisualElement itemRoot = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					borderBottomColor = Color.white,
					paddingLeft = 5f,
					paddingRight = 5f,
				},
			};

			// Use a custom manipulator for clicking and drag 'n drop to avoid callbacks conflicting with each other
			itemRoot.AddManipulator(new ItemManipulator());

			itemRoot.Add(new Image() {
				style = {
					maxWidth = 16f,
					maxHeight = 16f,
				},
			});

			itemRoot.Add(new Label() {
				style = {
					flexGrow = 1f,
					flexShrink = 1f,
					unityTextAlign = TextAnchor.MiddleLeft,
					overflow = Overflow.Hidden,
					textOverflow = TextOverflow.Ellipsis,
					unityTextOverflowPosition = TextOverflowPosition.Start,
					marginLeft = 5f,
				},
			});

			return itemRoot;
		}

		private void BindListItem(VisualElement element, int index) {
			Object @object = m_historyList[index];
			element.userData = index;

			element.style.opacity = index > SelectionManager.HistoryIndex ? 0.5f : 1f;
			element.style.borderBottomWidth = index == SelectionManager.HistoryIndex ? 1f : 0f;

			if (@object == null) {
				element.Q<Label>().text = "<color=yellow>Unknown</color>";
				return;
			}

			Texture icon = EditorGUIUtility.ObjectContent(@object, @object.GetType()).image;
			element.Q<Image>().image = icon;

			string label = AssetDatabase.Contains(@object)
				? AssetDatabase.GetAssetPath(@object)
				: @object.name;

			element.Q<Label>().text = label;
			element.tooltip = label;
		}

		private VisualElement CreateControlsGUI() {
			m_controlsElement = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f),
					flexShrink = 0f,
				},
			};

			VisualElement moveButtonsRoot = new VisualElement() {
				style = { flexDirection = FlexDirection.Row, },
			};
			Button backButton = new Button(SelectionManager.GoBack) { name = "Back" };
			backButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_tab_prev").image });
			moveButtonsRoot.Add(backButton);

			Button forwardButton = new Button(SelectionManager.GoForward) { name = "Forward" };
			forwardButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_tab_next").image });
			moveButtonsRoot.Add(forwardButton);

			m_controlsElement.Add(moveButtonsRoot);

			MaskField selectionTypesField = new MaskField(Enum.GetNames(typeof(SelectionTypes)).ToList(), (int)SelectionManager.AllowedSelections);
			selectionTypesField.RegisterValueChangedCallback(evt => SelectionManager.AllowedSelections = (SelectionTypes)evt.newValue);
			m_controlsElement.Add(selectionTypesField);

			Button clearButton = new Button(SelectionManager.ClearHistory) { name = "Clear" };
			clearButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image });
			m_controlsElement.Add(clearButton);

			RefreshControlsEnabled();

			return m_controlsElement;
		}

		private void OnHistoryChanged() {
			m_historyList.Clear();
			m_historyList.AddRange(SelectionManager.History);

			m_historyListView.RefreshItems();

			if (m_historyList.Count == 0) {
				RefreshEmptyListLabel();
				m_historyListView.style.visibility = Visibility.Visible;
			}
			else {
				// Allow the ListView items to update before scrolling
				EditorApplication.delayCall += () => {
					m_historyListView.ScrollToItem(SelectionManager.HistoryIndex);
					m_historyListView.style.visibility = Visibility.Visible;
				};
			}

			RefreshControlsEnabled();
		}

		private void RefreshEmptyListLabel() {
			Label label = m_historyListView.Q<Label>(null, "unity-list-view__empty-label");
			label.text = "History is Empty";
			label.style.flexGrow = 1f;
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.fontSize = 20f;
		}

		private void RefreshControlsEnabled() {
			m_controlsElement.Q<Button>("Back").SetEnabled(SelectionManager.ValidateGoBack());
			m_controlsElement.Q<Button>("Forward").SetEnabled(SelectionManager.ValidateGoForward());
			m_controlsElement.Q<Button>("Clear").SetEnabled(SelectionManager.ValidateClearHistory());
		}

		private void OnMovedInHistory(int index) {
			m_historyListView.SetSelectionWithoutNotify(Enumerable.Range(index, 1));
			m_historyListView.ScrollToItem(SelectionManager.HistoryIndex);
			m_historyListView.RefreshItems();

			RefreshControlsEnabled();
		}

		private class ItemManipulator : Manipulator {
			private bool m_isPointerDown;

			protected override void RegisterCallbacksOnTarget() {
				target.RegisterCallback<PointerDownEvent>(OnPointerDown);
				target.RegisterCallback<PointerUpEvent>(OnPointerUp);
				target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
			}

			protected override void UnregisterCallbacksFromTarget() {
				target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
				target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
				target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
			}

			private void OnPointerDown(PointerDownEvent evt) {
				if (m_isPointerDown) return;

				evt.StopImmediatePropagation();

				m_isPointerDown = true;
				target.CapturePointer(0);
			}

			private void OnPointerUp(PointerUpEvent evt) {
				if (!m_isPointerDown) return;

				evt.StopImmediatePropagation();

				OnClick();

				target.ReleasePointer(0);
				m_isPointerDown = false;
			}

			private void OnPointerLeave(PointerLeaveEvent evt) {
				if (!m_isPointerDown) return;

				evt.StopImmediatePropagation();

				OnDragStart();

				target.ReleasePointer(0);
				m_isPointerDown = false;
			}

			private void OnClick() {
				int index = (int)target.userData;
				SelectionManager.MoveToIndex(index);
			}

			private void OnDragStart() {
				Object @object = SelectionManager.History[(int)target.userData];
				if (@object == null)
					return;

				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new[] { @object };
				DragAndDrop.StartDrag("Selection History Drag");
			}
		}
	}
}
