using System;
using Jackey.Behaviours.Editor.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class SelectionDragger : MouseManipulator {
		private ISelectionManager m_manager;

		private bool m_active;
		private Vector2 m_start;
		private Vector3 m_startTransform;

		public event Action<VisualElement, Vector2, Vector2> Moved;

		public SelectionDragger(ISelectionManager manager) {
			m_manager = manager;

			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse
			});
		}

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}

		private void OnMouseDown(MouseDownEvent evt) {
			if (m_active)
			{
				evt.StopImmediatePropagation();
				return;
			}

			if (!CanStartManipulation(evt))
				return;

			ISelectableElement selectableTarget = evt.target as ISelectableElement;

			if (!m_manager.SelectedElements.Contains(selectableTarget))
				return;

			m_start = evt.localMousePosition;
			m_startTransform = target.transform.position;
			m_active = true;
			target.CaptureMouse();
			evt.StopImmediatePropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_active) return;

			Vector2 movement = evt.localMousePosition - m_start;

			foreach (ISelectableElement selection in m_manager.SelectedElements)
				selection.Element.transform.position += (Vector3)movement;

			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_active || !CanStopManipulation(evt))
				return;

			m_active = false;
			target.ReleaseMouse();
			evt.StopPropagation();

			Moved?.Invoke(target, m_startTransform, target.transform.position);
		}
	}
}
