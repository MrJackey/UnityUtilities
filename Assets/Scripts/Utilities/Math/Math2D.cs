using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Jackey.Utilities.Math {
	public static class Math2D {
		/// <summary>
		/// Get a Vector2 pointing at "<paramref name="to"/>" originating at "<paramref name="from"/>"
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 FromTo(Vector2 from, Vector2 to) => to - from;

		/// <summary>
		/// Get the distance between a and b squared
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float SqrDistance(Vector2 a, Vector2 b) => (a - b).sqrMagnitude;

		/// <summary>
		/// Get the distance between a and b
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;

		/// <summary>
		/// Clamp the values of a Vector2 within the range min..max
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Clamp(Vector2 vec, float min, float max) {
			return new Vector2(
				Mathf.Clamp(vec.x, min, max),
				Mathf.Clamp(vec.y, min, max)
			);
		}

		/// <summary>
		/// Clamp the values of a Vector2 within the range min..max
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Clamp(Vector2 vec, Vector2 min, Vector2 max) {
			return new Vector2(
				Mathf.Clamp(vec.x, min.x, max.x),
				Mathf.Clamp(vec.y, min.y, max.y)
			);
		}

		/// <summary>
		/// Copy a Vector2 setting each component less or equal to <paramref name="min"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Min(Vector2 vec, float min) {
			return new Vector2(
				Mathf.Min(vec.x, min),
				Mathf.Min(vec.y, min)
			);
		}

		/// <summary>
		/// Copy a Vector2 setting each component greater or equal to <paramref name="max"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Max(Vector2 vec, float max) {
			return new Vector2(
				Mathf.Max(vec.x, max),
				Mathf.Max(vec.y, max)
			);
		}

		/// <summary>
		/// Round each component of a Vector2 to the smallest integer that is greater or equal
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Ceil(Vector2 vec) {
			return new Vector2(
				Mathf.Ceil(vec.x),
				Mathf.Ceil(vec.y)
			);
		}

		/// <summary>
		/// Round each component of a Vector2 to the smallest integer greater or equal adding the result in a <see cref="Vector2Int"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2Int CeilToInt(Vector2 vec) {
			return new Vector2Int(
				Mathf.CeilToInt(vec.x),
				Mathf.CeilToInt(vec.y)
			);
		}

		/// <summary>
		/// Round each component of a Vector2 to the largest integer that is smaller or equal
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Floor(Vector2 vec) {
			return new Vector2(
				Mathf.Floor(vec.x),
				Mathf.Floor(vec.y)
			);
		}

		/// <summary>
		/// Round each component of a Vector2 to the largest integer that is smaller or equal adding the result in a <see cref="Vector2Int"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2Int FloorToInt(Vector2 vec) {
			return new Vector2Int(
				Mathf.FloorToInt(vec.x),
				Mathf.FloorToInt(vec.y)
			);
		}

		/// <summary>
		/// Round each component of a Vector2 to the nearest integer
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Round(Vector2 vec) {
			return new Vector2(
				Mathf.Round(vec.x),
				Mathf.Round(vec.y)
			);
		}

		/// <summary>
		/// Round each component of a Vector2 to the nearest integer adding the result in a <see cref="Vector2Int"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2Int RoundToInt(Vector2 vec) {
			return new Vector2Int(
				Mathf.RoundToInt(vec.x),
				Mathf.RoundToInt(vec.y)
			);
		}

		/// <summary>
		/// Create a new Vector2 with the signs (-1f, 1f) of the source Vector2 components. Note that 0f is seen as positive
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Sign(Vector2 vec) {
			return new Vector2(
				Mathf.Sign(vec.x),
				Mathf.Sign(vec.y)
			);
		}

		/// <summary>
		/// Create a new Vector2Int with the signs (-1, 1) of the source Vector2 components. Note that 0 is seen as positive
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2Int SignToInt(Vector2 vec) {
			return new Vector2Int(
				(int)Mathf.Sign(vec.x),
				(int)Mathf.Sign(vec.y)
			);
		}

		/// <summary>
		/// Create a new Vector2 with the signs (-1f, 0f, 1f) of the source Vector2 components
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 Sign0(Vector2 vec) {
			return new Vector2(
				System.Math.Sign(vec.x),
				System.Math.Sign(vec.y)
			);
		}

		/// <summary>
		/// Create a new Vector2Int with the signs (-1, 0, 1) of the source Vector2 components
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2Int Sign0ToInt(Vector2 vec) {
			return new Vector2Int(
				System.Math.Sign(vec.x),
				System.Math.Sign(vec.y)
			);
		}

		/// <summary>
		/// Get a new vector rotated 90 degrees clockwise
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 PerpendicularRight(Vector2 vec) => new Vector2(vec.y, -vec.x);

		/// <summary>
		/// Get a new vector rotated 90 degrees counter clockwise
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 PerpendicularLeft(Vector2 vec) => new Vector2(-vec.y, vec.x);

		/// <summary>
		/// Get a new vector representing <paramref name="vec"/> rotated
		/// <paramref name="angle"/> specified in radians
		/// </summary>
		/// <param name="vec">Vector to rotate</param>
		/// <param name="angle">Angle specified in radians</param>
		[Pure]
		public static Vector2 Rotate(Vector2 vec, float angle) {
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);

			return new Vector2(
				vec.x * cos - vec.y * sin,
				vec.x * sin + vec.y * cos
			);
		}

		/// <summary>
		/// Get a new vector representing <paramref name="vec"/> rotated around
		/// <paramref name="point"/> <paramref name="angle"/> specified in radians
		/// </summary>
		/// <param name="vec">Vector to rotate</param>
		/// <param name="point">Vector specifying rotation point</param>
		/// <param name="angle">Angle specified in radians</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static Vector2 RotateAroundPoint(Vector2 vec, Vector2 point, float angle) {
			return Rotate(vec - point, angle) + point;
		}

		/// <summary>
		/// Project a vector onto another vector
		/// </summary>
		[Pure]
		public static Vector2 Project(Vector2 vec, Vector2 onto) {
			Vector2 direction = onto.normalized;
			float dot = Vector2.Dot(vec, direction);

			return direction * dot;
		}
	}
}
