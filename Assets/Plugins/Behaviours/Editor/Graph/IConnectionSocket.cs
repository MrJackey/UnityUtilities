using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public interface IConnectionSocket {
		[NotNull] VisualElement Element { get; }
		Vector2 Tangent { get; set; }

		int MaxIncomingConnections { get; set; }
		int MaxOutgoingConnections { get; set; }
		int IncomingConnections { get; set; }
		int OutgoingConnections { get; set; }
	}

	public interface IConnectionAreaSocket : IConnectionSocket {
		Vector2 GetInPoint(Connection connection);
		Vector2 GetOutPoint(Connection connection);

		Vector2 GetInTangent(Vector2 point);
		Vector2 GetOutTangent(Vector2 point);
	}
}
