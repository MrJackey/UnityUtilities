using System;
using System.Collections.Generic;

namespace Jackey.Utilities.Extensions {
	public static class IListExtensions {
		/// <summary>
		/// Swap the content of two indices
		/// </summary>
		public static void Swap<T>(this IList<T> source, Index lhs, Index rhs) {
			(source[lhs], source[rhs]) = (source[rhs], source[lhs]);
		}
	}
}
