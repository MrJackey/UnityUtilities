using System.Collections.Generic;

namespace Jackey.Behaviours.Editor.Graph {
	public interface IConnectionSocketOwner {
		public List<IConnectionSocket> Sockets { get; }
	}
}
