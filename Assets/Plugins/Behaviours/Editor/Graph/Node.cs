using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class Node : VisualElement, ISelectableElement {
		public const float DEFAULT_WIDTH = 100f;
		public const float DEFAULT_HEIGHT = 64f;

		public override VisualElement contentContainer { get; }

		VisualElement ISelectableElement.Element => this;

		protected Node() {
			style.position = Position.Absolute;
			usageHints = UsageHints.DynamicTransform;

			hierarchy.Add(contentContainer = new VisualElement() {
				name = "Content",
				pickingMode = PickingMode.Ignore,
				style = { flexGrow = 1f },
			});

			AddToClassList(nameof(Node));
		}
	}
}
