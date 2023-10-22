using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class TransformExtensions {
		/// <summary>
		/// Destroy all children of the transform
		/// </summary>
		public static void Clear(this Transform source) {
			int childCount = source.childCount;

			for (int i = childCount - 1; i >= 0; i--) {
				Object.Destroy(source.GetChild(i).gameObject);
			}
		}
	}
}
