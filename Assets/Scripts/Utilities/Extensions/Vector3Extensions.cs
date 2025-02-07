using System.Runtime.CompilerServices;
using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class Vector3Extensions {
		/// <summary>
		/// Split up a Vector3 into its two composites: direction and magnitude
		/// </summary>
		public static void Decompose(this Vector3 vec, out Vector3 direction, out float magnitude) {
			magnitude = vec.magnitude;

			if (magnitude > float.Epsilon)
				direction = vec / magnitude;
			else
				direction = Vector3.zero;
		}

		/// <summary>
		/// Returns a Vector3 copy giving x a new value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 WithX(this Vector3 vec, float x) => new Vector3(x, vec.y, vec.z);

		/// <summary>
		/// Returns a Vector3 copy giving y a new value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 WithY(this Vector3 vec, float y) => new Vector3(vec.x, y, vec.z);

		/// <summary>
		/// Returns a Vector3 copy giving z a new value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 WithZ(this Vector3 vec, float z) => new Vector3(vec.x, vec.y, z);

		/// <summary>
		/// Returns a Vector3 copy giving x and y new values
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 WithXY(this Vector3 vec, float x, float y) => new Vector3(x, y, vec.z);

		/// <summary>
		/// Returns a Vector3 copy giving x and z new values
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 WithXZ(this Vector3 vec, float x, float z) => new Vector3(x, vec.y, z);

		/// <summary>
		/// Returns a Vector3 copy giving y and z new values
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 WithYZ(this Vector3 vec, float y, float z) => new Vector3(vec.x, y, z);
	}
}
