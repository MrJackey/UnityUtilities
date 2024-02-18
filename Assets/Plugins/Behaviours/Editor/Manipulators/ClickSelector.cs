﻿using Jackey.Behaviours.Editor.Graph;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class ClickSelector : MouseManipulator {
		private ISelectionManager m_manager;

		public ClickSelector(ISelectionManager manager) {
			m_manager = manager;

			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
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

			ISelectableElement selectableTarget = (ISelectableElement)target;

			if (m_manager.SelectedElements.Count == 1 && m_manager.SelectedElements.Contains(selectableTarget))
				return;

			m_manager.ClearSelection();
			m_manager.AddToSelection(selectableTarget);

			m_manager.OnSelectionChange();
		}
	}
}
