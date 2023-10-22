using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

public static class GlobalObjectExtensions {
	/// <summary>
	/// <para>
	/// Check whether a UnityEngine.Object object exists in both the c# and c++ environment.
	/// </para>
	/// <para>
	/// This ensures no internal lifetime checks are missed and tells if the object is safe to interact with.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Exists(this Object @object) {
		return @object != null;
	}

	/// <summary>
	/// <para>
	/// Get the object if it exists, otherwise it returns real null.
	/// </para>
	/// <para>
	/// This allows safe use of null propagation (?.) and null-coalescing (??) on
	/// UnityEngine.Object objects without bypassing their lifetime checks.
	/// </para>
	/// </summary>
	[CanBeNull]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T OrNull<T>(this T @object) where T : Object {
		return @object.Exists() ? @object : null;
	}
}
