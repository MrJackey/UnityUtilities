﻿using Jackey.Behaviours.Editor.Manipulators;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class GraphGroup : VisualElement, ISelectableElement, ITickElement {
		private const float AUTO_SIZE_PADDING_TOP = 85f;
		private const float AUTO_SIZE_PADDING_RIGHT = 50f;
		private const float AUTO_SIZE_PADDING_BOTTOM = 50f;
		private const float AUTO_SIZE_PADDING_LEFT = 50f;

		private TextField m_label;
		private Image m_autoSizeImage;

		private bool m_autoSize;

		private Resizer m_resizer;
		private Dragger m_dragger;

		public string Label {
			get => m_label.value;
			set => m_label.value = value;
		}
		public bool AutoSize => m_autoSize;

		VisualElement ISelectableElement.Element => this;

		public GraphGroup(Rect rect) {
			style.position = Position.Absolute;

			Reposition(rect);

			Add(m_label = new TextField());

			VisualElement statusBar = new VisualElement() { name = "StatusBar" };
			Add(statusBar);

			statusBar.Add(m_autoSizeImage = new Image() { name = "AutoSizeToggle", scaleMode = ScaleMode.ScaleToFit });
			m_autoSizeImage.AddManipulator(new Clickable(() => SetAutoSize(!m_autoSize)));

			m_resizer = new Resizer();
			m_dragger = new Dragger();

			SetAutoSize_Internal(m_autoSize);
		}

		void ITickElement.Tick() {
			if (AutoSize)
				UpdateAutoSize();
		}

		private void UpdateAutoSize() {
			Rect? totalRect = null;

			foreach (VisualElement sibling in parent.Children()) {
				if (sibling is not IGroupableElement)
					continue;

				Rect overlapRect = this.ChangeCoordinatesTo(sibling, layout);
				if (!sibling.Overlaps(overlapRect))
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

		private void Reposition(Rect rect) {
			transform.position = rect.min;
			style.width = rect.width;
			style.height = rect.height;
		}

		public void SetAutoSize(bool value) {
			if (value == m_autoSize) return;

			SetAutoSize_Internal(value);
		}

		private void SetAutoSize_Internal(bool value) {
			if (value) {
				this.RemoveManipulator(m_resizer);
				this.RemoveManipulator(m_dragger);
			}
			else {
				this.AddManipulator(m_resizer);
				this.AddManipulator(m_dragger);
			}

			m_autoSizeImage.image = Resources.Load<Texture>(value ? "GroupAutoSize_Enabled" : "GroupAutoSize_Disabled");
			m_autoSize = value;
		}
	}
}
