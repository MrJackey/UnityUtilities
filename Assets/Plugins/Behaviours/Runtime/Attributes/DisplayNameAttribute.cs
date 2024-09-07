using System;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Class)]
	public class DisplayNameAttribute : Attribute {
		public string Name { get; private set; }

		public DisplayNameAttribute(string name) {
			Name = name;
		}
	}
}
