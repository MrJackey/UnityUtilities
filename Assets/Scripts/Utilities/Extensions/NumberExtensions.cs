using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Jackey.Utilities.Extensions {
	public static class NumberExtensions {
		/// <summary>
		/// Shorthand for multiplying a float with itself
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static float Sqr(this float source) {
			return source * source;
		}

		/// <summary>
		/// Shorthand for multiplying an int with itself
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Pure]
		public static int Sqr(this int source) {
			return source * source;
		}
	}
}
