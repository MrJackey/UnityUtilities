using System;

namespace Jackey.Utilities.Extensions {
	public static class SpanExtensions {
		/// <summary>
		/// Swap the content of two indices
		/// </summary>
		public static void Swap<T>(this Span<T> source, Index lhs, Index rhs) {
			(source[lhs], source[rhs]) = (source[rhs], source[lhs]);
		}
	}
}
