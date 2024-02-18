using System.Collections.Generic;
using Jackey.Behaviours.Editor.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class RectangleSelector : MouseManipulator {
		private ISelectionManager m_manager;
		private VisualElement m_rectElement;

		private List<ISelectableElement> m_selectionBuffer = new();

		private bool m_active;
		private Vector2 m_start;
		private Vector2 m_end;

		public RectangleSelector(ISelectionManager manager) {
			m_manager = manager;

			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
			});

			m_rectElement = new VisualElement() {
				pickingMode = PickingMode.Ignore,
			};
			m_rectElement.AddToClassList("SelectionRect");
		}

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
			target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
			target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
		}

		private void OnMouseDown(MouseDownEvent evt) {
			if (m_active)
			{
				evt.StopImmediatePropagation();
			}
			else
			{
				if (!CanStartManipulation(evt))
					return;

				target.hierarchy.Add(m_rectElement);

				m_start = evt.localMousePosition;
				m_end = m_start;

				UpdateVisualRect();
				m_active = true;

				target.CaptureMouse();
				evt.StopImmediatePropagation();
			}
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_active)
				return;

			m_end = evt.localMousePosition;

			UpdateVisualRect();
			UpdatePreSelection();

			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_active)
				return;

			if (!CanStopManipulation(evt))
				return;

			// Collect selectable elements within rect
			Rect selectionRect = GetSelectionRect();

			foreach (VisualElement child in m_manager.Element.Children()) {
				if (child is not ISelectableElement selectable)
					continue;

				m_manager.RemoveFromPreSelection(selectable);

				Rect childRect = target.ChangeCoordinatesTo(child, selectionRect);

				if (!child.Overlaps(childRect))
					continue;

				m_selectionBuffer.Add(selectable);
			}

			// Reset the manipulation
			m_rectElement.RemoveFromHierarchy();
			m_active = false;
			target.ReleaseMouse();
			evt.StopPropagation();

			// Check if selection changed
			if (m_selectionBuffer.Count == m_manager.SelectedElements.Count) {
				// No selection -> No selection
				if (m_selectionBuffer.Count == 0)
					return;

				// The selection stays the same
				bool newSelection = false;
				foreach (ISelectableElement bufferSelectable in m_selectionBuffer) {
					if (!m_manager.SelectedElements.Contains(bufferSelectable)) {
						newSelection = true;
						break;
					}
				}

				if (!newSelection) {
					m_selectionBuffer.Clear();
					return;
				}
			}

			// Pass on the selection to the manager
			m_manager.ClearSelection();
			m_manager.AddToSelection(m_selectionBuffer);
			m_selectionBuffer.Clear();

			m_manager.OnSelectionChange();
		}

		private void UpdateVisualRect() {
			Rect rect = GetSelectionRect();

			m_rectElement.transform.position = rect.min;
			m_rectElement.style.width = rect.width;
			m_rectElement.style.height = rect.height;
	}

		private void UpdatePreSelection() {
			Rect selectionRect = GetSelectionRect();

			foreach (VisualElement child in m_manager.Element.Children()) {
				if (child is not ISelectableElement selectable)
					continue;

				Rect childRect = target.ChangeCoordinatesTo(child, selectionRect);

				if (child.Overlaps(childRect))
					m_manager.AddToPreSelection(selectable);
				else
					m_manager.RemoveFromPreSelection(selectable);
			}
		}

		private Rect GetSelectionRect() {
			Rect rect = new Rect(m_start, Vector2.zero);
			rect.xMin = Mathf.Min(m_start.x, m_end.x);
			rect.xMax = Mathf.Max(m_start.x, m_end.x);
			rect.yMin = Mathf.Min(m_start.y, m_end.y);
			rect.yMax = Mathf.Max(m_start.y, m_end.y);

			return rect;
		}

		private void OnMouseCaptureOutEvent(MouseCaptureOutEvent evt) {
			if (!m_active)
				return;

			m_rectElement.RemoveFromHierarchy();
			m_active = false;
		}
	}
}
