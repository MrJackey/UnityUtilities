using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Class)]
	[Conditional("UNITY_EDITOR")]
	public class GraphIconAttribute : Attribute {
		private static Dictionary<string, Texture> s_iconCache = new();

		public string Path { get; set; }

		public GraphIconAttribute(string path) {
			Path = path;
		}

		[CanBeNull]
		public static Texture GetTexture(Type type) {
			GraphIconAttribute iconAttribute = (GraphIconAttribute)type.GetCustomAttribute(typeof(GraphIconAttribute));

			if (iconAttribute == null)
				return null;

			if (s_iconCache.TryGetValue(iconAttribute.Path, out Texture iconTexture))
				return iconTexture;

			iconTexture = Resources.Load<Texture>(iconAttribute.Path);

			if (iconTexture != null)
				s_iconCache.Add(iconAttribute.Path, iconTexture);

			return iconTexture;
		}
	}
}
