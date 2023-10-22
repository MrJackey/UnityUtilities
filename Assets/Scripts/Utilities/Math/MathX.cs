using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Jackey.Utilities.Math {
	public static class MathX {
		/// <summary>
		/// Get the differentiation between <paramref name="from"/> and <paramref name="to"/>.
		/// The sign is decided as if moving <paramref name="from"/> -> <paramref name="to"/>.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float FromTo(float from, float to) => to - from;

		/// <inheritdoc cref="FromTo(float,float)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float FromTo(int from, int to) => to - from;

		/// <summary>
		/// Remap a value from one range into another range
		/// </summary>
		/// <param name="low1">Lower boundary of the initial range</param>
		/// <param name="high1">Upper boundary of the initial range</param>
		/// <param name="low2">Lower boundary of the new range</param>
		/// <param name="high2">Upper boundary of the new range</param>
		/// <remarks>
		/// Note that the output is not clamped and can therefore return a value outside the range
		///	<paramref name="low2"/>..<paramref name="high2"/> if <paramref name="t"/> is outside
		/// the range <paramref name="low1"/>..<paramref name="high1"/>
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Remap(float low1, float high1, float low2, float high2, float t) {
			return low2 + (t - low1) * (high2 - low2) / (high1 - low1);
		}

		/// <inheritdoc cref="Remap"/>
		/// <remarks>
		/// The output will be clamped to the new range
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float RemapClamped(float low1, float high1, float low2, float high2, float t) {
			return Remap(low1, high1, low2, high2, Mathf.Clamp(t, low1, high1));
		}

		/// <summary>
		/// Rounds a value to the nearest value of a constant interval. Values at the exact center are rounded upwards.
		/// </summary>
		/// <returns>Returns the value snapped to the specified interval</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Snap(float value, float interval) {
			return Mathf.Round(value / interval) * interval;
		}

		/// <summary>
		/// Get the sign (-1f, 1f) of a float. Note that 0f is seen as positive
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Sign(float value) {
			return Mathf.Sign(value);
		}

		/// <summary>
		/// Get the sign (-1f, 1f) of a float as a int. Note that 0f is seen as positive
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static int SignToInt(float value) {
			return (int)Mathf.Sign(value);
		}

		/// <summary>
		/// Get the sign (-1f, 0f, 1f) of a float
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Sign0(float vec) {
			return System.Math.Sign(vec);
		}

		/// <summary>
		/// Get the sign (-1f, 0f, 1f) of a float as a int
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Sign0ToInt(float value) {
			return System.Math.Sign(value);
		}

		/// <summary>
		/// Compares two floating point values to see if they are either similar or the first value is greater
		/// </summary>
		/// <returns>Returns true if the first value is greater or approximate to the second value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static bool GreaterOrApproximate(float value, float comparand) {
			return value >= comparand || Mathf.Approximately(value, comparand);
		}

		/// <summary>
		/// Compares two floating point values to see if they are either similar or the first value is less
		/// </summary>
		/// <returns>Returns true if the first value is less or approximate to the second value</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static bool LessOrApproximate(float value, float comparand) {
			return value <= comparand || Mathf.Approximately(value, comparand);
		}
	}
}
