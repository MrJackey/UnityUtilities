using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class Resizer : MouseManipulator {
		private const float EDGE_THICKNESS = 15f;

		private VisualElement[] m_edgeElements = new VisualElement[8];

		private bool m_active;
		private Edges m_activeEdges;

		private Vector2 m_pointerStart;
		private Vector3 m_startPosition;
		private Vector2 m_startSize;

		public float MinWidth { get; set; } = 50f;
		public float MinHeight { get; set; } = 50f;

		public event Action Resized;

		public Resizer() {
			activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
			});
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);

			target.hierarchy.Add(m_edgeElements[0] = CreateEdgeElement(Edges.Top));
			target.hierarchy.Add(m_edgeElements[1] = CreateEdgeElement(Edges.Right));
			target.hierarchy.Add(m_edgeElements[2] = CreateEdgeElement(Edges.Bottom));
			target.hierarchy.Add(m_edgeElements[3] = CreateEdgeElement(Edges.Left));
			target.hierarchy.Add(m_edgeElements[4] = CreateEdgeElement(Edges.TopRight));
			target.hierarchy.Add(m_edgeElements[5] = CreateEdgeElement(Edges.BottomRight));
			target.hierarchy.Add(m_edgeElements[6] = CreateEdgeElement(Edges.BottomLeft));
			target.hierarchy.Add(m_edgeElements[7] = CreateEdgeElement(Edges.TopLeft));
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

			for (int i = 0; i < m_edgeElements.Length; i++) {
				Debug.Assert(m_edgeElements[i] != null);
				m_edgeElements[i].RemoveFromHierarchy();
				m_edgeElements[i] = null;
			}
		}

		private VisualElement CreateEdgeElement(Edges edge) {
			VisualElement element = new VisualElement { style = { position = Position.Absolute } };

			switch (edge) {
				case Edges.Top:
				case Edges.Bottom:
					element.style.height = EDGE_THICKNESS;
					element.AddToClassList("ResizeVertical");
					break;
				case Edges.Left:
				case Edges.Right:
					element.style.width = EDGE_THICKNESS;
					element.AddToClassList("ResizeHorizontal");
					break;
				case Edges.TopRight:
				case Edges.BottomLeft:
					element.style.height = EDGE_THICKNESS;
					element.style.width = EDGE_THICKNESS;
					element.AddToClassList("ResizeUpRight");
					break;
				case Edges.BottomRight:
				case Edges.TopLeft:
					element.style.height = EDGE_THICKNESS;
					element.style.width = EDGE_THICKNESS;
					element.AddToClassList("ResizeUpLeft");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
			}

			return element;
		}

		private void OnMouseDown(MouseDownEvent evt) {
			if (!CanStartManipulation(evt)) return;
			if (!IsOnEdge(evt, out Edges edges)) return;

			// Get initial values
			m_pointerStart = GetPointerPosition(evt);
			m_startPosition = target.transform.position;
			m_startSize = target.layout.size;
			m_activeEdges = edges;

			// Start manipulation
			m_active = true;
			target.CaptureMouse();
			evt.StopImmediatePropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_active) {
				// Update edges to show the correct cursor. This isn't possible when registering callbacks as the styling hasn't been applied yet.
				// The border width is therefore not available causing it to be offset from the checked edges.
				UpdateEdges();
				return;
			}

			Vector2 pointerOffset = GetPointerPosition(evt) - m_pointerStart;
			Vector2 positionOffset = Vector2.zero;

			if ((m_activeEdges & Edges.Top) != 0) {
				target.style.height = Mathf.Max(m_startSize.y - pointerOffset.y, MinHeight);
				positionOffset.y = Mathf.Min(pointerOffset.y, m_startSize.y - MinHeight);
			}

			if ((m_activeEdges & Edges.Bottom) != 0)
				target.style.height = Mathf.Max(m_startSize.y + pointerOffset.y, MinHeight);

			if ((m_activeEdges & Edges.Left) != 0) {
				target.style.width = Mathf.Max(m_startSize.x - pointerOffset.x, MinWidth);
				positionOffset.x = Mathf.Min(pointerOffset.x, m_startSize.x - MinWidth);
			}

			if ((m_activeEdges & Edges.Right) != 0)
				target.style.width = Mathf.Max(m_startSize.x + pointerOffset.x, MinWidth);

			// Position offset must be accumulated as you can drag the top-left corner, moving on both the x and y-axis
			target.transform.position = m_startPosition + (Vector3)positionOffset;

			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_active || !CanStopManipulation(evt))
				return;

			m_active = false;
			target.ReleaseMouse();
			evt.StopPropagation();

			Resized?.Invoke();
		}

		private Vector2 GetPointerPosition(IMouseEvent evt) {
			// Transforming the local position to the parent is required when resizing upwards or to the left. Because the element
			// is moved to compensate the style values only affecting a single direction, the local value becomes unreliable
			return target.ChangeCoordinatesTo(target.hierarchy.parent, evt.localMousePosition);
		}

		private bool IsOnEdge(IMouseEvent evt, out Edges edges) {
			edges = 0;

			Vector2 localPosition = evt.localMousePosition;
			Rect localBound = target.localBound;

			if (localPosition.x <= EDGE_THICKNESS)
				edges |= Edges.Left;

			if (localBound.width - localPosition.x <= EDGE_THICKNESS)
				edges |= Edges.Right;

			if (localPosition.y <= EDGE_THICKNESS)
				edges |= Edges.Top;

			if (localBound.height - localPosition.y <= EDGE_THICKNESS)
				edges |= Edges.Bottom;

			return edges != 0;
		}

		private void UpdateEdges() {
			IResolvedStyle resolvedStyle = target.resolvedStyle;

			for (int i = 0; i < m_edgeElements.Length; i++) {
				VisualElement element = m_edgeElements[i];

				switch ((Edges)(1 << i)) {
					case Edges.Top:
						element.style.top = -resolvedStyle.borderTopWidth;
						element.style.left = -resolvedStyle.borderLeftWidth;
						element.style.width = resolvedStyle.width;
						break;
					case Edges.Bottom:
						element.style.left = -resolvedStyle.borderLeftWidth;
						element.style.bottom = -resolvedStyle.borderBottomWidth;
						element.style.width = resolvedStyle.width;
						break;
					case Edges.Right:
						element.style.top = -resolvedStyle.borderTopWidth;
						element.style.right = -resolvedStyle.borderRightWidth;
						element.style.height = resolvedStyle.height;
						break;
					case Edges.Left:
						element.style.top = -resolvedStyle.borderTopWidth;
						element.style.left = -resolvedStyle.borderRightWidth;
						element.style.height = resolvedStyle.height;
						break;
					case Edges.TopRight:
						element.style.top = -resolvedStyle.borderTopWidth;
						element.style.right = -resolvedStyle.borderRightWidth;
						break;
					case Edges.BottomRight:
						element.style.bottom = -resolvedStyle.borderBottomWidth;
						element.style.right = -resolvedStyle.borderRightWidth;
						break;
					case Edges.BottomLeft:
						element.style.bottom = -resolvedStyle.borderBottomWidth;
						element.style.left = -resolvedStyle.borderRightWidth;
						break;
					case Edges.TopLeft:
						element.style.top = -resolvedStyle.borderTopWidth;
						element.style.left = -resolvedStyle.borderRightWidth;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		[Flags]
		private enum Edges {
			Top = 1 << 0,
			Right = 1 << 1,
			Bottom = 1 << 2,
			Left = 1 << 3,
			TopRight = 1 << 4,
			BottomRight = 1 << 5,
			BottomLeft = 1 << 6,
			TopLeft = 1 << 7,
		}
	}
}
