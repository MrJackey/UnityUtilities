using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public interface IConnectionSocket {
		[NotNull] VisualElement Element { get; }
		Vector2 Tangent { get; }

		int MaxIncomingConnections { get; set; }
		int MaxOutgoingConnections { get; set; }
		int IncomingConnections { get; set; }
		int OutgoingConnections { get; set; }
	}
}
