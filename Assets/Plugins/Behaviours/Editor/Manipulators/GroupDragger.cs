using System;
using System.Collections.Generic;
using Jackey.Behaviours.Editor.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class GroupDragger : MouseManipulator {
		private GraphGroup m_group;
		private List<IGroupableElement> m_groupElements = new();

		private bool m_active;
		private Vector2 m_start;
		private Vector2 m_startTransform;

		public bool Active => m_active;

		public event Action<VisualElement, Vector2, Vector2> Moved;

		public GroupDragger(GraphGroup group) {
			m_group = group;

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
			if (Active)
			{
				evt.StopImmediatePropagation();
				return;
			}

			if (!CanStartManipulation(evt))
				return;

			m_group.GetGroupedElements(m_groupElements);

			m_start = evt.localMousePosition;
			m_startTransform = target.transform.position;
			m_active = true;
			target.CaptureMouse();
			evt.StopImmediatePropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!Active) return;

			Vector3 movement = (Vector3)(evt.localMousePosition - m_start);

			m_group.transform.position += movement;
			foreach (IGroupableElement element in m_groupElements)
				element.Element.transform.position += movement;

			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!Active || !CanStopManipulation(evt))
				return;

			m_groupElements.Clear();

			m_active = false;
			target.ReleaseMouse();
			evt.StopPropagation();

			Moved?.Invoke(target, m_startTransform, target.transform.position);
		}
	}
}
