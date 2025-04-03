using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class Dragger : MouseManipulator {
		private bool m_active;
		private Vector2 m_start;

		public bool ConstrainToParent { get; set; } = false;

		public Dragger() {
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

			m_start = evt.localMousePosition;
			m_active = true;
			target.CaptureMouse();
			evt.StopPropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_active) return;

			Vector2 movement = evt.localMousePosition - m_start;

			if (ConstrainToParent) {
				VisualElement parent = target.parent;

				if (parent != null) {
					Rect rectInParent = target.localBound;
					Rect parentBound = parent.localBound;

					rectInParent.position += movement;

					if (rectInParent.x < 0f)
						movement.x -= rectInParent.x;
					else if (rectInParent.xMax > parentBound.width)
						movement.x -= rectInParent.xMax - parentBound.width;

					if (rectInParent.y < 0f)
						movement.y -= rectInParent.y;
					else if (rectInParent.yMax > parentBound.height)
						movement.y -= rectInParent.yMax - parentBound.height;
				}
			}

			target.transform.position += (Vector3)movement;

			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_active || !CanStopManipulation(evt))
				return;

			m_active = false;
			target.ReleaseMouse();
			evt.StopPropagation();
		}
	}
}
