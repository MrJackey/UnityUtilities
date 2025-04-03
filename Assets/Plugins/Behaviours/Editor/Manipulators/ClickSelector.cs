using Jackey.Behaviours.Editor.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class ClickSelector : MouseManipulator {
		private ISelectionManager m_manager;

		public ClickSelector(ISelectionManager manager) {
			m_manager = manager;

			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
			});
			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
				modifiers = EventModifiers.Control,
			});
		}

		protected override void RegisterCallbacksOnTarget() {
			if (target is not ISelectableElement)
				return;

			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
		}

		protected override void UnregisterCallbacksFromTarget() {
			if (target is not ISelectableElement)
				return;

			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
		}

		private void OnMouseDown(MouseDownEvent evt) {
			if (!CanStartManipulation(evt)) return;

			evt.StopPropagation();

			ISelectableElement selectableTarget = (ISelectableElement)target;

			if ((evt.modifiers & EventModifiers.Control) == 0) {
				if (m_manager.SelectedElements.Count == 1 && m_manager.SelectedElements.Contains(selectableTarget))
					return;

				m_manager.ReplaceSelection(selectableTarget);
			}
			else {
				if (selectableTarget is not IGroupSelectable)
					return;

				if (m_manager.SelectedElements.Contains(selectableTarget))
					m_manager.RemoveFromSelection(selectableTarget);
				else
					m_manager.AddToSelection(selectableTarget);
			}

			m_manager.OnSelectionChange();
		}
	}
}
