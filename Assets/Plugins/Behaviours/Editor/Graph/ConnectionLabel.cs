using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class ConnectionLabel : Label, ISelectableElement {
		public VisualElement Element => this;
	}
}
