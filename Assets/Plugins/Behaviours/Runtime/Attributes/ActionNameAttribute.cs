using System;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Class)]
	public class ActionNameAttribute : Attribute {
		public string Name { get; private set; }

		public ActionNameAttribute(string name) {
			Name = name;
		}
	}
}
