using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class ContentDragger : MouseManipulator {
		private bool m_active;
		private Vector2 m_start;

		public ContentDragger() {
			m_active = false;
			activators.Add(new ManipulatorActivationFilter()
			{
				button = MouseButton.LeftMouse,
				modifiers = EventModifiers.Alt
			});
			activators.Add(new ManipulatorActivationFilter()
			{
				button = MouseButton.MiddleMouse
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

    protected void OnMouseDown(MouseDownEvent evt) {
	    if (m_active)
      {
        evt.StopImmediatePropagation();
        return;
      }

	    if (!CanStartManipulation(evt))
		    return;

	    m_start = target.ChangeCoordinatesTo(target.contentContainer, evt.localMousePosition);
	    m_active = true;
	    target.CaptureMouse();
	    evt.StopImmediatePropagation();
    }

    protected void OnMouseMove(MouseMoveEvent evt)
    {
      if (!m_active)
        return;

      VisualElement content = target.contentContainer;

      Vector2 offset = target.ChangeCoordinatesTo(content, evt.localMousePosition) - m_start;
      Vector3 scale = content.transform.scale;
      content.transform.position += Vector3.Scale((Vector3)offset, scale);
      evt.StopPropagation();
    }

    protected void OnMouseUp(MouseUpEvent evt)
    {
      if (!m_active || !CanStopManipulation(evt))
        return;

      m_active = false;
      target.ReleaseMouse();
      evt.StopPropagation();
    }
	}
}
