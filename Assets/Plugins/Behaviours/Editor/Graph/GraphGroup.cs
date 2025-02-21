using Jackey.Behaviours.Editor.Manipulators;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class GraphGroup : VisualElement, ISelectableElement {
		private TextField m_label;

		public string Label {
			get => m_label.value;
			set => m_label.value = value;
		}

		VisualElement ISelectableElement.Element => this;

		public GraphGroup(Rect rect) {
			style.position = Position.Absolute;

			transform.position = rect.min;
			style.width = rect.width;
			style.height = rect.height;

			Add(m_label = new TextField());

			this.AddManipulator(new Resizer());
			this.AddManipulator(new Dragger());
		}
	}
}
