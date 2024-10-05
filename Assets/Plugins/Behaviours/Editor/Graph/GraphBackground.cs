using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class GraphBackground : ImmediateModeElement {
		private static Material s_material;

		public float Spacing { get; set; } = 50f;

		public GraphBackground() {
			this.StretchToParentSize();
		}

		protected override void ImmediateRepaint() {
			if (Spacing == 0f)
				return;

			Vector3 position = parent.contentContainer.transform.position;
			Vector3 scale = parent.contentContainer.transform.scale;

			Vector2 start = new Vector2(
				position.x % (Spacing * scale.x),
				position.y % (Spacing * scale.y)
			);
			Rect bound = localBound;

			if (s_material == null)
				s_material = new Material(Shader.Find("Hidden/Internal-Colored"));

			s_material.SetPass(0);

			GL.PushMatrix();
			GL.Begin(GL.LINES);

			GL.Color(new Color(0.16f, 0.16f, 0.16f, 1f));

			float xStep = Spacing * scale.x;
			for (float x = start.x; x < bound.width; x += xStep) {
				GL.Vertex3(x, 0f, 0f);
				GL.Vertex3(x, bound.height, 0f);
			}

			float yStep = Spacing * scale.y;
			float startY = start.y > 0f ? start.y : start.y + yStep; // < 0 draws on the tab itself. It should only draw inside the actual window
			for (float y = startY; y < bound.height; y += yStep) {
				GL.Vertex3(0f, y, 0f);
				GL.Vertex3(bound.width, y, 0f);
			}

			GL.End();
			GL.PopMatrix();
		}
	}
}
