using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class ContentZoomer : Manipulator {
		public float Step { get; set; } = 0.05f;
		public float MinZoom { get; set; } = 1f;
		public float MaxZoom { get; set; } = 0.25f;

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<WheelEvent>(OnWheel);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<WheelEvent>(OnWheel);
		}

		private void OnWheel(WheelEvent evt) {
			VisualElement content = target.contentContainer;
			Vector2 localMousePosition = evt.localMousePosition;

			Vector3 position = content.transform.position;
			Vector3 scale = content.transform.scale;
			float zoom = scale.x;

			Vector2 contentLocalMousePosition = target.ChangeCoordinatesTo(content, localMousePosition);

			float newZoom = Mathf.Clamp(zoom - evt.delta.y * Step, MaxZoom, MinZoom);
			Vector3 newScale = new Vector3(newZoom, newZoom, 1f);
			content.transform.scale = newScale;

			Vector2 scaledTargetLocalMousePosition = content.ChangeCoordinatesTo(target, contentLocalMousePosition);
			Vector3 newPosition = position + (Vector3)(localMousePosition - scaledTargetLocalMousePosition);
			content.transform.position = newPosition;
		}
	}
}
