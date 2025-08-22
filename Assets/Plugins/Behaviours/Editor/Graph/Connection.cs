using Jackey.Behaviours.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class Connection : ImmediateModeElement {
		private const float TANGENT_WEIGHT = 30f;
		private const float WIDTH = 10f;
		private const float CLICK_DISTANCE = 10f;

		public IConnectionSocket Start { get; set; }
		public IConnectionSocket End { get; set; }

		public Connection() {
			style.position = Position.Absolute;
		}

		public Connection(IConnectionSocket start, IConnectionSocket end) : this() {
			Start = start;
			End = end;

			start.OutgoingConnections++;
			end.IncomingConnections++;

			usageHints = UsageHints.DynamicTransform;
		}

		protected override void ImmediateRepaint() {
			(Vector2 start, Vector2 end) = GetPoints();

			float width = Mathf.Max(Mathf.Abs(start.x - end.x), WIDTH);
			float height = Mathf.Max(Mathf.Abs(start.y - end.y), WIDTH);

			Vector2 localCenter = (start + end) / 2f;
			Vector2 size = new Vector2(width, height);
			Rect localRect = new Rect(
				localCenter - size / 2f - new Vector2(CLICK_DISTANCE, CLICK_DISTANCE),
				size + new Vector2(CLICK_DISTANCE * 2f, CLICK_DISTANCE * 2f)
			);

			// Prevent drawing and updating of the transform if the result won't be visible.
			// DrawSolidDisc is worst with ~0.035ms each, with two per connection
			if (!parent.Overlaps(this.ChangeCoordinatesTo(parent, localRect)))
				return;

			// Update bounds. Add click distance as padding to handle edges being clicked
			transform.position = this.ChangeCoordinatesTo(parent.contentContainer, localRect).position;
			style.width = width + 2f * CLICK_DISTANCE;
			style.height = height + 2f * CLICK_DISTANCE;

			// Draw graphics
			(Vector2 startTangent, Vector2 endTangent) = GetTangents(start, end);

			Handles.DrawBezier(
				start,
				end,
				start + startTangent,
				end + endTangent,
				new Color(0.5f, 0.5f, 0.5f, 1f),
				null,
				WIDTH
			);

			Handles.color = new Color(0.5f, 0.5f, 0.5f, 1f);
			Handles.DrawSolidDisc(end, Vector3.back, WIDTH * 0.75f);
			Handles.color = Color.white;
		}

		public IConnectionSocket GetClosestSocket(MouseDownEvent evt) {
			(Vector2 start, Vector2 end) = GetPoints();
			Vector2 mousePosition = ((VisualElement)evt.target).ChangeCoordinatesTo(this, evt.localMousePosition);

			if (Vector2.SqrMagnitude(start - mousePosition) < Vector2.SqrMagnitude(end - mousePosition))
				return Start;

			return End;
		}

		private (Vector2 start, Vector2 end) GetPoints() {
			Debug.Assert(Start != null || End != null);

			Vector2 mousePosition = Event.current?.mousePosition ?? Vector2.zero;

			Vector2 start = mousePosition;
			if (Start != null) {
				VisualElement startElement = Start.Element;

				if (startElement is IConnectionAreaSocket areaSocket)
					start = startElement.ChangeCoordinatesTo(this, areaSocket.GetPoint(this));
				else
					start = startElement.ChangeCoordinatesTo(this, startElement.GetLocalOrigin());
			}

			Vector2 end = mousePosition;
			if (End != null) {
				VisualElement endElement = End.Element;

				if (endElement is IConnectionAreaSocket areaSocket)
					end = endElement.ChangeCoordinatesTo(this, areaSocket.GetPoint(this));
				else
					end = endElement.ChangeCoordinatesTo(this, endElement.GetLocalOrigin());
			}

			return (start, end);
		}

		private (Vector2 startTangent, Vector2 endTangent) GetTangents(Vector2 start, Vector2 end) {
			Vector2 mousePosition = Event.current?.mousePosition ?? Vector2.zero;

			Vector2 startTangent;
			if (Start != null)
				startTangent = Start.Tangent;
			else
				startTangent = (end - mousePosition).normalized;

			Vector2 endTangent;
			if (End != null)
				endTangent = End.Tangent;
			else
				endTangent = (start - mousePosition).normalized;

			float weight = Mathf.Min(TANGENT_WEIGHT, Vector2.Distance(start, end) / 2f);
			return (startTangent * weight, endTangent * weight);
		}

		public override bool ContainsPoint(Vector2 localPoint) {
			(Vector2 start, Vector2 end) = GetPoints();
			(Vector2 startTangent, Vector2 endTangent) = GetTangents(start, end);

			float distance = HandleUtility.DistancePointBezier(
				localPoint,
				start,
				end,
				start + startTangent,
				end + endTangent
			);

			return distance <= CLICK_DISTANCE;
		}
	}
}
