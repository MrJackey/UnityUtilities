using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class ConnectionSocket : VisualElement, IConnectionSocket {
		public int MaxIncomingConnections { get; set; } = -1;
		public int MaxOutgoingConnections { get; set; } = -1;

		public int IncomingConnections { get; set; }
		public int OutgoingConnections { get; set; }

		VisualElement IConnectionSocket.Element => this;
		public Vector2 Tangent { get; set; }

		public ConnectionSocket() {
			style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(50f));
		}
	}
}
