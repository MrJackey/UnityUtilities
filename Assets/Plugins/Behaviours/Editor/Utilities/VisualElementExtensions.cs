using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Utilities {
	public static class VisualElementExtensions {
		public static Vector3 GetLocalOrigin(this VisualElement element) {
			Vector3 local = Vector3.zero;
			TransformOrigin origin = element.style.transformOrigin.value;
			Rect bound = element.localBound;

			local.x += origin.x.unit switch {
				LengthUnit.Pixel => origin.x.value,
				LengthUnit.Percent => (origin.x.value / 100f) * bound.width,
				_ => throw new ArgumentOutOfRangeException(),
			};
			local.y += origin.y.unit switch {
				LengthUnit.Pixel => origin.y.value,
				LengthUnit.Percent => (origin.y.value / 100f) * bound.height,
				_ => throw new ArgumentOutOfRangeException(),
			};

			return local;
		}

		public static void EnsureClass(this VisualElement element, string className) {
			if (element.ClassListContains(className))
				return;

			element.AddToClassList(className);
		}
	}
}
