using System;
using System.Diagnostics;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
	[Conditional("UNITY_EDITOR")]
	public class SearchPathAttribute : Attribute {
		public readonly string Path;

		public SearchPathAttribute(string path) {
			Path = path;
		}
	}
}
