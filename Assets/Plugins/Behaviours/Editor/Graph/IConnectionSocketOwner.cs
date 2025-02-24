using System.Collections.Generic;

namespace Jackey.Behaviours.Editor.Graph {
	public interface IConnectionSocketOwner {
		List<IConnectionSocket> Sockets { get; }
	}
}
