using System;
using System.Reflection;
using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.Utilities {
	public static class TypeExtensions {
		public static string GetDisplayOrTypeName(this Type type) {
			DisplayNameAttribute nameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
			return nameAttribute?.Name ?? type.Name;
		}
	}
}
