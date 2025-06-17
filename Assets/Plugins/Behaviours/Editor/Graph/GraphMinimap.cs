using Jackey.Behaviours.Editor.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class GraphMinimap : ImmediateModeElement {
		private const float PADDING = 8f;
		private const float HORIZONTAL_RATIO = 1f + 0.38f;
		private const float VERTICAL_RATIO = 1f - 0.38f;

		private static Material s_material;
		private static readonly Color s_groupColor = new Color(0.137f, 0.137f, 0.137f);
		private static readonly Color s_connectionColor = new Color(0.5f, 0.5f, 0.5f);
		private static readonly Color s_nodeColor = new Color(0.30f, 0.30f, 0.30f);
		private static readonly Color s_viewportColor = new Color(1f, 1f, 1f);

		private readonly BehaviourGraph m_graph;

		private bool m_mouseDown;

		public GraphMinimap(BehaviourGraph graph) {
			style.position = Position.Absolute;

			m_graph = graph;
			RegisterCallback<MouseDownEvent>(OnMouseDown);
			RegisterCallback<MouseMoveEvent>(OnMouseMove);
			RegisterCallback<MouseUpEvent>(OnMouseUp);
			RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

			if (s_material == null)
				s_material = new Material(Shader.Find("Hidden/Internal-Colored"));
		}

		protected override void ImmediateRepaint() {
			Rect mapRect = UpdateMapRect();
			Vector2 display = layout.size;
			display -= new Vector2(PADDING * 2f, PADDING * 2f);

			s_material.SetPass(0);
			GL.PushMatrix();

			DrawGroups(display, mapRect);
			DrawConnections(display, mapRect);
			DrawNodes(display, mapRect);
			DrawViewport(display, mapRect);

			GL.PopMatrix();
		}

		private Rect UpdateMapRect() {
			Rect mapRect = GetLocalMapRect();
			float ratio = mapRect.width / mapRect.height;

			SetRatioClass(ratio switch {
				> HORIZONTAL_RATIO => "Horizontal",
				< VERTICAL_RATIO => "Vertical",
				_ => "Square",
			});

			return mapRect;

			void SetRatioClass(string className) {
				if (ClassListContains(className)) return;

				RemoveFromClassList("Square");
				RemoveFromClassList("Horizontal");
				RemoveFromClassList("Vertical");

				AddToClassList(className);
			}
		}

		private Rect GetLocalMapRect() {
			Rect viewportRect = new Rect(-m_graph.contentContainer.transform.position, m_graph.layout.size);

			if (m_graph.contentContainer.childCount == 0)
				return viewportRect.ExpandToRatio(1f);

			Rect? contRect = null;
			foreach (VisualElement child in m_graph.contentContainer.Children()) {
				contRect = contRect?.Encapsulate(child.localBound) ?? child.localBound;
			}

			Debug.Assert(contRect.HasValue);

			const float VIEWPORT_MULTIPLIER = 1.15f;
			const float HALF_VIEWPORT_MULTIPLIER = VIEWPORT_MULTIPLIER * 0.5f;

			Rect content = contRect.Value;
			float ratio = content.width * content.height > 200_000f ? content.width / content.height : 1f;
			float scale = m_graph.contentContainer.transform.scale.x;

			Rect mapRect = content.Encapsulate(new Rect(
				content.center - (viewportRect.size * HALF_VIEWPORT_MULTIPLIER) / scale,
				(viewportRect.size * VIEWPORT_MULTIPLIER) / scale)
			);

			return ratio switch {
				> HORIZONTAL_RATIO => mapRect.ExpandToRatio(16f / 9f),
				< VERTICAL_RATIO => mapRect.ExpandToRatio(9f / 16f),
				_ => mapRect.ExpandToRatio(1f),
			};
		}

		private void DrawGroups(Vector2 display, Rect mapRect) {
			GL.Begin(GL.QUADS);
			GL.Color(s_groupColor);

			foreach (GraphGroup group in m_graph.Groups) {
				DrawQuad(group.localBound, display, mapRect);
			}

			GL.End();
		}

		private void DrawConnections(Vector2 display, Rect mapRect) {
			GL.Begin(GL.LINES);
			GL.Color(s_connectionColor);

			Vector2 mapPosition = mapRect.position;
			Vector2 mapSize = mapRect.size;

			VisualElement container = m_graph.contentContainer;

			foreach (Connection connection in m_graph.Connections) {
				if (connection.Start == null || connection.End == null)
					continue;

				VisualElement startElement = connection.Start.Element;
				VisualElement endElement = connection.End.Element;

				Vector2 start = startElement.ChangeCoordinatesTo(container, startElement.GetLocalOrigin()) - mapPosition;
				Vector2 end = endElement.ChangeCoordinatesTo(container, endElement.GetLocalOrigin()) - mapPosition;

				float startX = (start.x / mapSize.x) * display.x + PADDING;
				float startY = (start.y / mapSize.y) * display.y + PADDING;

				float endX = (end.x / mapSize.x) * display.x + PADDING;
				float endY = (end.y / mapSize.y) * display.y + PADDING;

				GL.Vertex3(startX, startY, 0f);
				GL.Vertex3(endX, endY, 0f);
			}

			GL.End();
		}

		private void DrawNodes(Vector2 display, Rect mapRect) {
			GL.Begin(GL.QUADS);
			GL.Color(s_nodeColor);

			foreach (Node node in m_graph.Nodes) {
				DrawQuad(node.localBound, display, mapRect);
			}

			GL.End();
		}

		private void DrawViewport(Vector2 display, Rect mapRect) {
			GL.Begin(GL.LINES);
			GL.Color(s_viewportColor);

			Vector2 mapPosition = mapRect.position;
			Vector2 mapSize = mapRect.size;
			Vector2 viewSize = m_graph.layout.size;

			Rect bound = m_graph.ChangeCoordinatesTo(m_graph.contentContainer, new Rect(Vector2.zero, viewSize));
			bound.position -= mapPosition;

			float leftX = (bound.xMin / mapRect.width) * display.x + PADDING;
			float rightX = leftX + (bound.width / mapSize.x) * display.x;
			float topY = (bound.yMin / mapRect.height) * display.y + PADDING;
			float bottomY = topY + (bound.height / mapSize.y) * display.y;

			const float MIN_SIZE = 10f;
			float xMax = display.x + PADDING;
			float yMax = display.y + PADDING;
			float clampedLeftX = Mathf.Clamp(leftX, PADDING, xMax - MIN_SIZE);
			float clampedRightX = Mathf.Clamp(rightX, PADDING + MIN_SIZE, xMax);
			float clampedTopY = Mathf.Clamp(topY, PADDING, yMax - MIN_SIZE);
			float clampedBottomY = Mathf.Clamp(bottomY, PADDING + MIN_SIZE, yMax);

			// Top
			if (topY >= PADDING) {
				GL.Vertex3(clampedLeftX, clampedTopY, 0f);
				GL.Vertex3(clampedRightX, clampedTopY, 0f);
			}

			// Right
			if (rightX <= xMax) {
				GL.Vertex3(clampedRightX, clampedTopY, 0f);
				GL.Vertex3(clampedRightX, clampedBottomY, 0f);
			}

			// Bottom
			if (bottomY <= yMax) {
				GL.Vertex3(clampedLeftX, clampedBottomY, 0f);
				GL.Vertex3(clampedRightX, clampedBottomY, 0f);
			}

			// Left
			if (leftX >= PADDING) {
				GL.Vertex3(clampedLeftX, clampedTopY, 0f);
				GL.Vertex3(clampedLeftX, clampedBottomY, 0f);
			}

			// Center Cross
			// float centerX = (leftX + rightX) * 0.5f;
			// float centerY = (topY + bottomY) * 0.5f;
			// GL.Vertex3(centerX, topY, 0f);
			// GL.Vertex3(centerX, bottomY, 0f);
			// GL.Vertex3(leftX, centerY, 0f);
			// GL.Vertex3(rightX, centerY, 0f);

			GL.End();
		}

		private void DrawQuad(Rect bound, Vector2 display, Rect mapRect) {
			bound.position -= mapRect.position;
			Vector2 mapSize = mapRect.size;

			float leftX = (bound.xMin / mapSize.x) * display.x + PADDING;
			float rightX = leftX + (bound.width / mapSize.x) * display.x;
			float topY = (bound.yMin / mapSize.y) * display.y + PADDING;
			float bottomY = topY + (bound.height / mapSize.y) * display.y;

			GL.Vertex3(leftX, topY, 0f);
			GL.Vertex3(rightX, topY, 0f);
			GL.Vertex3(rightX, bottomY, 0f);
			GL.Vertex3(leftX, bottomY, 0f);
		}

		private void OnMouseDown(MouseDownEvent evt) {
			evt.StopImmediatePropagation();

			if (m_mouseDown) return;
			if (m_graph.contentContainer.childCount == 0) return;

			this.CaptureMouse();
			m_mouseDown = true;

			MoveOnEvent(evt.localMousePosition);
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_mouseDown) return;

			MoveOnEvent(evt.localMousePosition);
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_mouseDown) return;

			evt.StopPropagation();

			this.ReleaseMouse();
			m_mouseDown = false;
		}

		private void OnMouseLeave(MouseLeaveEvent evt) {
			if (!m_mouseDown) return;

			this.ReleaseMouse();
			m_mouseDown = false;
		}

		private void MoveOnEvent(Vector2 localMousePosition) {
			Rect mapRect = GetLocalMapRect();

			Vector2 display = layout.size;
			display -= new Vector2(PADDING * 2f, PADDING * 2f);

			Vector2 normalizedMousePosition = new Vector2(
				Mathf.Clamp01((localMousePosition.x - PADDING) / display.x),
				Mathf.Clamp01(1f - (localMousePosition.y - PADDING) / display.y)
			);

			Vector2 offset = new Vector2(
				mapRect.width * normalizedMousePosition.x,
				mapRect.height * (1f - normalizedMousePosition.y)
			);

			Vector2 center = m_graph.contentContainer.ChangeCoordinatesTo(m_graph, mapRect.position + offset);
			Vector2 graphSize = m_graph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - center;

			m_graph.contentContainer.transform.position += (Vector3)centerOffset;
		}
	}
}
