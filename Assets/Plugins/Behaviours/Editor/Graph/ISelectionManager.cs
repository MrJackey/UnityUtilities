using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public interface ISelectionManager {
		const string PRE_SELECTED_CLASS = "PreSelection";
		const string SELECTED_CLASS = "Selected";

		[NotNull] VisualElement Element { get; }
		List<ISelectableElement> SelectedElements { get; }

		void OnSelectionChange();
	}

	public static class SelectionManagerExtensions {
		public static void ClearSelection(this ISelectionManager manager) {
			foreach (ISelectableElement selectableElement in manager.SelectedElements)
				selectableElement.Element.RemoveFromClassList(ISelectionManager.SELECTED_CLASS);

			manager.SelectedElements.Clear();
		}

		public static void AddToPreSelection(this ISelectionManager _, ISelectableElement element) {
			if (element.Element.ClassListContains(ISelectionManager.PRE_SELECTED_CLASS))
				return;

			element.Element.AddToClassList(ISelectionManager.PRE_SELECTED_CLASS);
		}

		public static void RemoveFromPreSelection(this ISelectionManager _, ISelectableElement element) {
			element.Element.RemoveFromClassList(ISelectionManager.PRE_SELECTED_CLASS);
		}

		public static void AddToSelection(this ISelectionManager manager, ISelectableElement element) {
			element.Element.AddToClassList(ISelectionManager.SELECTED_CLASS);
			manager.SelectedElements.Add(element);
		}

		public static void AddToSelection(this ISelectionManager manager, List<ISelectableElement> elements) {
			foreach (ISelectableElement selectableElement in elements)
				selectableElement.Element.AddToClassList(ISelectionManager.SELECTED_CLASS);

			manager.SelectedElements.AddRange(elements);
		}

		public static void AddToSelection(this ISelectionManager manager, IEnumerable<ISelectableElement> elements) {
			foreach (ISelectableElement selectableElement in elements)
				selectableElement.Element.AddToClassList(ISelectionManager.SELECTED_CLASS);

			manager.SelectedElements.AddRange(elements);
		}

		public static void RemoveFromSelection(this ISelectionManager manager, ISelectableElement element) {
			element.Element.RemoveFromClassList(ISelectionManager.SELECTED_CLASS);
			manager.SelectedElements.Remove(element);
		}

		public static void ReplaceSelection(this ISelectionManager manager, ISelectableElement element) {
			manager.ClearSelection();
			manager.AddToSelection(element);
			manager.OnSelectionChange();
		}

		public static void ReplaceSelection(this ISelectionManager manager, List<ISelectableElement> elements) {
			manager.ClearSelection();
			manager.AddToSelection(elements);
			manager.OnSelectionChange();
		}

		public static void ReplaceSelection(this ISelectionManager manager, IEnumerable<ISelectableElement> elements) {
			manager.ClearSelection();
			manager.AddToSelection(elements);
			manager.OnSelectionChange();
		}
	}
}
