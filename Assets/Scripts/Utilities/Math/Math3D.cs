using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Jackey.Utilities.Math {
	public static class Math3D {
		/// <summary>
		/// Get a Vector3 pointing at "<paramref name="to"/>" originating at "<paramref name="from"/>"
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 FromTo(Vector3 from, Vector3 to) => to - from;

		/// <summary>
		/// Get the distance between a and b squared
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float SqrDistance(Vector3 a, Vector3 b) => (a - b).sqrMagnitude;

		/// <summary>
		/// Get the distance between a and b
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;

		/// <summary>
		/// Clamp the values of a Vector3 within the range min..max
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Clamp(Vector3 vec, float min, float max) {
			return new Vector3(
				Mathf.Clamp(vec.x, min, max),
				Mathf.Clamp(vec.y, min, max),
				Mathf.Clamp(vec.z, min, max)
			);
		}

		/// <summary>
		/// Clamp the values of a Vector3 within the range min..max
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Clamp(Vector3 vec, Vector3 min, Vector3 max) {
			return new Vector3(
				Mathf.Clamp(vec.x, min.x, max.x),
				Mathf.Clamp(vec.y, min.y, max.y),
				Mathf.Clamp(vec.z, min.z, max.z)
			);
		}

		/// <summary>
		/// Copy a Vector3 setting each component less or equal to <paramref name="min"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Min(Vector3 vec, float min) {
			return new Vector3(
				Mathf.Min(vec.x, min),
				Mathf.Min(vec.y, min),
				Mathf.Min(vec.z, min)
			);
		}

		/// <summary>
		/// Copy a Vector3 setting each component less or equal to their corresponding counterpart in <paramref name="min"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Min(Vector3 vec, Vector3 min) {
			return new Vector3(
				Mathf.Min(vec.x, min.x),
				Mathf.Min(vec.y, min.y),
				Mathf.Min(vec.z, min.z)
			);
		}

		/// <summary>
		/// Copy a Vector3 setting each component greater or equal to <paramref name="max"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Max(Vector3 vec, float max) {
			return new Vector3(
				Mathf.Max(vec.x, max),
				Mathf.Max(vec.y, max),
				Mathf.Max(vec.z, max)
			);
		}

		/// <summary>
		/// Copy a Vector3 setting each component greater or equal to their corresponding counterpart in <paramref name="max"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Max(Vector3 vec, Vector3 max) {
			return new Vector3(
				Mathf.Max(vec.x, max.x),
				Mathf.Max(vec.y, max.y),
				Mathf.Max(vec.z, max.z)
			);
		}

		/// <summary>
		/// Round each component of a Vector3 to the smallest integer that is greater or equal
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Ceil(Vector3 vec) {
			return new Vector3(
				Mathf.Ceil(vec.x),
				Mathf.Ceil(vec.y),
				Mathf.Ceil(vec.z)
			);
		}

		/// <summary>
		/// Round each component of a Vector3 to the smallest integer that is greater or equal adding the result in a <see cref="Vector3Int"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3Int CeilToInt(Vector3 vec) {
			return new Vector3Int(
				Mathf.CeilToInt(vec.x),
				Mathf.CeilToInt(vec.y),
				Mathf.CeilToInt(vec.z)
			);
		}

		/// <summary>
		/// Round each component of a Vector3 to the largest integer that is smaller or equal
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Floor(Vector3 vec) {
			return new Vector3(
				Mathf.Floor(vec.x),
				Mathf.Floor(vec.y),
				Mathf.Floor(vec.z)
			);
		}

		/// <summary>
		/// Round each component of a Vector3 to the largest integer that is smaller or equal adding the result in a <see cref="Vector3Int"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3Int FloorToInt(Vector3 vec) {
			return new Vector3Int(
				Mathf.FloorToInt(vec.x),
				Mathf.FloorToInt(vec.y),
				Mathf.FloorToInt(vec.z)
			);
		}

		/// <summary>
		/// Round each component of a Vector3 to the nearest integer
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Round(Vector3 vec) {
			return new Vector3(
				Mathf.Round(vec.x),
				Mathf.Round(vec.y),
				Mathf.Round(vec.z)
			);
		}

		/// <summary>
		/// Round each component of a Vector3 to the nearest integer adding the result in a <see cref="Vector3Int"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3Int RoundToInt(Vector3 vec) {
			return new Vector3Int(
				Mathf.RoundToInt(vec.x),
				Mathf.RoundToInt(vec.y),
				Mathf.RoundToInt(vec.z)
			);
		}

		/// <summary>
		/// Create a new Vector2 with the signs (-1f, 1f) of the source Vector2 components. Note that 0f is seen as positive
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Sign(Vector3 vec) {
			return new Vector3(
				Mathf.Sign(vec.x),
				Mathf.Sign(vec.y),
				Mathf.Sign(vec.z)
			);
		}

		/// <summary>
		/// Create a new Vector3Int with the signs (-1, 1) of the source Vector3 components. Note that 0 is seen as positive
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3Int SignToInt(Vector3 vec) {
			return new Vector3Int(
				(int)Mathf.Sign(vec.x),
				(int)Mathf.Sign(vec.y),
				(int)Mathf.Sign(vec.z)
			);
		}

		/// <summary>
		/// Create a new Vector3 with the signs (-1f, 0f, 1f) of the source Vector3 components
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3 Sign0(Vector3 vec) {
			return new Vector3(
				System.Math.Sign(vec.x),
				System.Math.Sign(vec.y),
				System.Math.Sign(vec.z)
			);
		}

		/// <summary>
		/// Create a new Vector3Int with the signs (-1, 0, 1) of the source Vector3 components
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector3Int Sign0ToInt(Vector3 vec) {
			return new Vector3Int(
				System.Math.Sign(vec.x),
				System.Math.Sign(vec.y),
				System.Math.Sign(vec.z)
			);
		}
	}
}
