using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public interface ISelectableElement {
		[NotNull] VisualElement Element { get; }
	}
}
