using System;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Class)]
	public class GraphIconAttribute : Attribute {
		public string Path { get; set; }

		public GraphIconAttribute(string path) {
			Path = path;
		}
	}
}
