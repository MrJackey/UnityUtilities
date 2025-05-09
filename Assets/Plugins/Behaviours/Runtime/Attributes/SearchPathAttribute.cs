﻿using System;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
	public class SearchPathAttribute : Attribute {
		public readonly string Path;

		public SearchPathAttribute(string path) {
			Path = path;
		}
	}
}
