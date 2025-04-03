using System.Runtime.CompilerServices;
using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class Vector2Extensions {
		/// <summary>
		/// Split up a Vector2 into its two composites: direction and magnitude
		/// </summary>
		public static void Decompose(this Vector2 vec, out Vector2 direction, out float magnitude) {
			magnitude = vec.magnitude;

			if (magnitude > float.Epsilon)
				direction = vec / magnitude;
			else
				direction = Vector2.zero;
		}

		/// <summary>
		/// Returns a Vector2 copy giving x a new value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 WithX(this Vector2 vec, float x) => new Vector2(x, vec.y);

		/// <summary>
		/// Returns a Vector2 copy giving y a new value
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 WithY(this Vector2 vec, float y) => new Vector2(vec.x, y);
	}
}
