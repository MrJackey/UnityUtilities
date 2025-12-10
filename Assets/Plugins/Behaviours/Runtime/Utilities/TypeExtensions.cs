using System;
using System.Reflection;
using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.Utilities {
	public static class TypeExtensions {
		public static string Editor_GetDisplayOrTypeName(this Type type) {
			DisplayNameAttribute nameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();

#if UNITY_EDITOR
			return nameAttribute?.Name ?? UnityEditor.ObjectNames.NicifyVariableName(type.Name);
#else
			return nameAttribute?.Name ?? type.Name;
#endif
		}
	}
}
