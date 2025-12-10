using Jackey.Behaviours.Editor.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class GraphGroupCreator : MouseManipulator {
		private GraphGroup m_group;

		private bool m_active;
		private Vector2 m_start;
		private Vector2 m_end;

		public delegate void GroupCreatedHandler(GraphGroup group);
		public event GroupCreatedHandler GroupCreated;

		public GraphGroupCreator() {
			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
				modifiers = EventModifiers.Control,
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
			if (m_active) {
				evt.StopImmediatePropagation();
				return;
			}

			if (!CanStartManipulation(evt))
				return;

			m_start = GetMousePosition(evt);
			m_group = new GraphGroup(new Rect(m_start, new Vector2(0f, 0f)));

			target.contentContainer.Add(m_group);
			m_group.SendToBack();

			m_active = true;
			target.CaptureMouse();
			evt.StopImmediatePropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_active) return;

			Rect rect = GetDragRect(evt);
			m_group.transform.position = rect.min;
			m_group.style.width = rect.width;
			m_group.style.height = rect.height;
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_active) return;
			if (!CanStopManipulation(evt)) return;

			// Release capture
			target.ReleaseMouse();
			evt.StopPropagation();
			m_active = false;

			GroupCreated?.Invoke(m_group);
			m_group = null;
		}

		private Vector2 GetMousePosition(IMouseEvent evt) {
			// Convert to content space to handle it being moved or scaled in relation to the viewport
			return target.ChangeCoordinatesTo(target.contentContainer, evt.localMousePosition);
		}

		private Rect GetDragRect(IMouseEvent evt) {
			Vector2 end = GetMousePosition(evt);

			Rect rect = new Rect(m_start, Vector2.zero);
			rect.xMin = Mathf.Min(m_start.x, end.x);
			rect.xMax = Mathf.Max(m_start.x, end.x);
			rect.yMin = Mathf.Min(m_start.y, end.y);
			rect.yMax = Mathf.Max(m_start.y, end.y);

			return rect;
		}
	}
}
