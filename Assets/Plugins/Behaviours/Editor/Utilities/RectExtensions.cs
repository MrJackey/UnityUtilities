using System.Diagnostics.Contracts;
using UnityEngine;

namespace Jackey.Behaviours.Editor.Utilities {
	public static class RectExtensions {
		[Pure]
		public static Rect Encapsulate(this Rect rect, Rect other) {
			rect.xMin = Mathf.Min(rect.xMin, other.xMin);
			rect.xMax = Mathf.Max(rect.xMax, other.xMax);
			rect.yMin = Mathf.Min(rect.yMin, other.yMin);
			rect.yMax = Mathf.Max(rect.yMax, other.yMax);
			return rect;
		}

		[Pure]
		public static Rect ExpandToRatio(this Rect rect, float ratio) {
			float rectRatio = rect.width / rect.height;

			if (rectRatio > ratio) {
				float delta = rect.width / ratio - rect.height;
				rect.y -= delta * 0.5f;
				rect.height += delta;
			}
			else if (rectRatio < ratio) {
				float delta = ratio * rect.height - rect.width;
				rect.x -= delta * 0.5f;
				rect.width += delta;
			}

			return rect;
		}
	}
}
