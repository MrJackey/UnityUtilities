using System;
using System.Collections.Generic;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class GraphGroup : VisualElement, ISelectableElement, ITickElement {
		private const float AUTO_SIZE_PADDING_TOP = 85f;
		private const float AUTO_SIZE_PADDING_RIGHT = 50f;
		private const float AUTO_SIZE_PADDING_BOTTOM = 50f;
		private const float AUTO_SIZE_PADDING_LEFT = 50f;

		private ISelectionManager m_selectionManager;

		private TextField m_label;
		private Image m_autoSizeImage;

		private bool m_autoSize;

		private Resizer m_resizer;
		private Dragger m_dragger;
		private GroupDragger m_groupDragger;

		public string Label {
			get => m_label.value;
			set => m_label.SetValueWithoutNotify(value);
		}
		public bool AutoSize => m_autoSize;

		public ISelectionManager SelectionManager {
			set => m_selectionManager = value;
		}

		VisualElement ISelectableElement.Element => this;

		public Dragger Dragger => m_dragger;
		public GroupDragger GroupDragger => m_groupDragger;
		public Resizer Resizer => m_resizer;

		public event Action Modified;

		public GraphGroup(Rect rect) {
			usageHints = UsageHints.DynamicTransform;

			style.position = Position.Absolute;

			Reposition(rect);

			Add(m_label = new TextField() { isDelayed = true });
			m_label.RegisterValueChangedCallback(evt => Modified?.Invoke());

			VisualElement statusBar = new VisualElement() { name = "StatusBar" };
			Add(statusBar);

			statusBar.Add(m_autoSizeImage = new Image() { name = "AutoSizeToggle", scaleMode = ScaleMode.ScaleToFit });
			m_autoSizeImage.AddManipulator(new Clickable(() => {
				SetAutoSize(!m_autoSize);
				Modified?.Invoke();
			}));

			m_resizer = new Resizer();
			m_dragger = new Dragger();
			m_groupDragger = new GroupDragger(this);

			Clickable doubleCLickManipulator = new Clickable(SelectGroupedElements);
			doubleCLickManipulator.activators.Clear();
			doubleCLickManipulator.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, clickCount = 2 });
			this.AddManipulator(doubleCLickManipulator);

			SetAutoSize_Internal(m_autoSize);
		}

		void ITickElement.Tick() {
			if (m_autoSize && !m_groupDragger.Active)
				UpdateAutoSize();
		}

		private void UpdateAutoSize() {
			Rect myLayout = layout;
			Rect? totalRect = null;

			foreach (VisualElement sibling in parent.Children()) {
				if (!IsGroupable(myLayout, sibling, out IGroupableElement _))
					continue;

				Rect siblingRect = sibling.localBound;
				if (totalRect == null) {
					totalRect = siblingRect;
					continue;
				}

				Rect rectWithSibling = new Rect(totalRect.Value);
				rectWithSibling.xMin = Mathf.Min(rectWithSibling.xMin, siblingRect.xMin);
				rectWithSibling.xMax = Mathf.Max(rectWithSibling.xMax, siblingRect.xMax);
				rectWithSibling.yMin = Mathf.Min(rectWithSibling.yMin, siblingRect.yMin);
				rectWithSibling.yMax = Mathf.Max(rectWithSibling.yMax, siblingRect.yMax);
				totalRect = rectWithSibling;
			}

			if (totalRect != null) {
				Rect rect = totalRect.Value;
				rect.min -= new Vector2(AUTO_SIZE_PADDING_LEFT, AUTO_SIZE_PADDING_TOP);
				rect.max += new Vector2(AUTO_SIZE_PADDING_RIGHT, AUTO_SIZE_PADDING_BOTTOM);
				Reposition(rect);
			}
		}

		public void Reposition(Rect rect) {
			transform.position = rect.min;
			style.width = rect.width;
			style.height = rect.height;
		}

		private void SelectGroupedElements() {
			Debug.Assert(m_selectionManager != null);

			bool hadSelection = m_selectionManager.SelectedElements.Count > 0;
			m_selectionManager.ClearSelection();

			Rect myLayout = layout;
			foreach (VisualElement sibling in parent.Children()) {
				if (!IsGroupable(myLayout, sibling, out IGroupableElement _))
					continue;

				if (sibling is not ISelectableElement selectable)
					continue;

				if (sibling is not IGroupSelectable)
					continue;

				m_selectionManager.AddToSelection(selectable);
			}

			if (hadSelection || m_selectionManager.SelectedElements.Count > 0)
				m_selectionManager.OnSelectionChange();
		}

		public void GetGroupedElements(List<IGroupableElement> list) {
			Rect myLayout = layout;

			foreach (VisualElement sibling in parent.Children()) {
				if (!IsGroupable(myLayout, sibling, out IGroupableElement groupable))
					continue;

				list.Add(groupable);
			}
		}

		public void SetAutoSize(bool value) {
			if (value == m_autoSize) return;

			SetAutoSize_Internal(value);
		}

		private void SetAutoSize_Internal(bool value) {
			if (value) {
				this.RemoveManipulator(m_resizer);
				this.RemoveManipulator(m_dragger);
				this.AddManipulator(m_groupDragger);

				UpdateAutoSize();
			}
			else {
				this.AddManipulator(m_resizer);
				this.AddManipulator(m_dragger);
				this.RemoveManipulator(m_groupDragger);
			}

			m_autoSizeImage.image = Resources.Load<Texture>(value ? "GroupAutoSize_Enabled" : "GroupAutoSize_Disabled");
			m_autoSize = value;
		}

		private bool IsGroupable(Rect myLayout, VisualElement element, out IGroupableElement groupable) {
			groupable = null;

			if (element is not IGroupableElement checkedGroupable)
				return false;

			Rect overlapRect = this.ChangeCoordinatesTo(element, myLayout);
			if (!element.Overlaps(overlapRect))
				return false;

			groupable = checkedGroupable;
			return true;
		}
	}
}
