using System;
using System.Collections.Generic;

namespace Jackey.Utilities.Extensions {
	public static class ListExtensions {
		/// <summary>
		/// Swaps the index of the item to be removed with the index of the last item in the list before removing it.
		/// </summary>
		/// <remarks>
		/// This reduces shifting of items to O(1) rather than O(n)
		/// </remarks>
		public static bool RemoveAsLast<T>(this List<T> source, T item) {
			int itemIndex = source.IndexOf(item);

			if (itemIndex == -1)
				return false;

			source.RemoveAtAsLast(itemIndex);
			return true;
		}

		/// <inheritdoc cref="RemoveAsLast{T}(System.Collections.Generic.IList{T},T)"/>
		public static void RemoveAtAsLast<T>(this List<T> source, Index index) {
			int lastIndex = source.Count - 1;

			source.Swap(index, lastIndex);
			source.RemoveAt(lastIndex);
		}
	}
}
