using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class RectExtensions {
		/// <summary>
		/// Grow the Rect to include the point
		/// </summary>
		public static void Encapsulate(ref this Rect rect, Vector2 point) {
			rect.xMin = Mathf.Min(rect.xMin, point.x);
			rect.xMax = Mathf.Max(rect.xMax, point.x);

			rect.yMin = Mathf.Min(rect.yMin, point.y);
			rect.yMax = Mathf.Max(rect.yMax, point.y);
		}
	}
}
