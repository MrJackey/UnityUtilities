using System;
using System.Reflection;
using Jackey.Behaviours.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Behaviours.Utilities {
	public static class TypeExtensions {
#if UNITY_EDITOR
		public static string Editor_GetDisplayOrTypeName(this Type type) {
			DisplayNameAttribute nameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
			return nameAttribute?.Name ?? ObjectNames.NicifyVariableName(type.Name);
		}
#endif
	}
}
