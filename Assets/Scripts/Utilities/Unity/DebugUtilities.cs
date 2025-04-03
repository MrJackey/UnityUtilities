using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Jackey.Utilities.Unity {
	public static class DebugUtilities {
		private static List<Vector3> s_points;

		#region Arrow

		[Conditional("UNITY_EDITOR")]
		public static void DrawArrow(Vector3 from, Vector3 direction) => DrawArrow(from, direction, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawArrow(Vector3 from, Vector3 direction, Color color) => DrawArrow(from, direction, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawArrow(Vector3 from, Vector3 direction, Color color, float duration) => DrawArrow(from, direction, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawArrow(Vector3 from, Vector3 direction, Color color, float duration, bool depthTest) {
			Camera camera = Camera.current;

			if (camera == null)
				return;

			Debug.DrawRay(from, direction, color, duration, depthTest);

			Vector3 point = from + direction;
			Vector3 forward = direction.normalized;
			Vector3 up = -camera.transform.forward;
			Vector3 right = Vector3.Cross(forward, -up);
			Matrix4x4 matrix = new Matrix4x4(right, up, forward, new Vector4(point.x, point.y, point.z, 1f));

			const float SIZE = 0.5f;
			Debug.DrawLine(point, matrix.MultiplyPoint3x4(new Vector3(SIZE, 0f, -SIZE)), color, duration, depthTest);
			Debug.DrawLine(point, matrix.MultiplyPoint3x4(new Vector3(-SIZE, 0f, -SIZE)), color, duration, depthTest);
		}

		#endregion

		#region Rect

		[Conditional("UNITY_EDITOR")]
		public static void DrawRect(Rect rect) => DrawRect(rect, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawRect(Rect rect, Color color) => DrawRect(rect, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawRect(Rect rect, Color color, float duration) => DrawRect(rect, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawRect(Rect rect, Color color, float duration, bool depthTest) {
			Debug.DrawRay(rect.min, new Vector3(rect.width, 0f), color, duration, depthTest);
			Debug.DrawRay(rect.min, new Vector3(0f, rect.height), color, duration, depthTest);
			Debug.DrawRay(rect.max, new Vector3(-rect.width, 0f), color, duration, depthTest);
			Debug.DrawRay(rect.max, new Vector3(0f, -rect.height), color, duration, depthTest);
		}

		#endregion

		#region Cube

		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size) => DrawCube(center, size, Quaternion.identity, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Color color) => DrawCube(center, size, Quaternion.identity, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Color color, float duration) => DrawCube(center, size, Quaternion.identity, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Color color, float duration, bool depthTest) => DrawCube(center, size, Quaternion.identity, color, duration, depthTest);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Quaternion orientation) => DrawCube(center, size, orientation, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Quaternion orientation, Color color) => DrawCube(center, size, orientation, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Quaternion orientation, Color color, float duration) => DrawCube(center, size, orientation, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCube(Vector3 center, Vector3 size, Quaternion orientation, Color color, float duration, bool depthTest) {
			Vector3 extents = size / 2f;

			Vector3 bottomCloseLeft = center - orientation * extents;
			Vector3 bottomCloseRight = bottomCloseLeft + orientation * new Vector3(size.x, 0f, 0f);

			Vector3 topCloseLeft = bottomCloseLeft + orientation * new Vector3(0f, size.y, 0f);
			Vector3 topCloseRight = bottomCloseRight + orientation * new Vector3(0f, size.y, 0f);

			Vector3 topFarRight = center + orientation * extents;
			Vector3 topFarLeft = topFarRight - orientation * new Vector3(size.x, 0f, 0f);

			Vector3 bottomFarLeft = topFarLeft - orientation * new Vector3(0f, size.y, 0f);
			Vector3 bottomFarRight = topFarRight - orientation * new Vector3(0f, size.y, 0f);

			Debug.DrawLine(bottomCloseLeft, bottomCloseRight, color, duration, depthTest);
			Debug.DrawLine(bottomCloseLeft, topCloseLeft, color, duration, depthTest);
			Debug.DrawLine(bottomCloseLeft, bottomFarLeft, color, duration, depthTest);

			Debug.DrawLine(topCloseLeft, topCloseRight, color, duration, depthTest);
			Debug.DrawLine(topCloseLeft, topFarLeft, color, duration, depthTest);

			Debug.DrawLine(topFarRight, topFarLeft, color, duration, depthTest);
			Debug.DrawLine(topFarRight, bottomFarRight, color, duration, depthTest);
			Debug.DrawLine(topFarRight, topCloseRight, color, duration, depthTest);

			Debug.DrawLine(bottomFarRight, bottomFarLeft, color, duration, depthTest);
			Debug.DrawLine(bottomFarRight, bottomCloseRight, color, duration, depthTest);

			Debug.DrawLine(bottomCloseRight, topCloseRight, color, duration, depthTest);
			Debug.DrawLine(bottomFarLeft, topFarLeft, color, duration, depthTest);
		}

		#endregion

		#region Hemisphere

		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius) => DrawHemisphere(center, radius, Quaternion.identity, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Color color) => DrawHemisphere(center, radius, Quaternion.identity, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Color color, float duration) => DrawHemisphere(center, radius, Quaternion.identity, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Color color, float duration, bool depthTest) => DrawHemisphere(center, radius, Quaternion.identity, color, duration, depthTest);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Quaternion orientation) => DrawHemisphere(center, radius, orientation, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Quaternion orientation, Color color) => DrawHemisphere(center, radius, orientation, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Quaternion orientation, Color color, float duration) => DrawHemisphere(center, radius, orientation, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawHemisphere(Vector3 center, float radius, Quaternion orientation, Color color, float duration, bool depthTest) {
			DrawArc(center, orientation * Vector3.up, center + orientation * new Vector3(radius, 0f, 0f), 360f, color, duration, depthTest);
			DrawArc(center, orientation * Vector3.forward, center + orientation * new Vector3(radius, 0f, 0f), 180f, color, duration, depthTest);
			DrawArc(center, orientation * Vector3.right, center + orientation * new Vector3(0f, 0f, -radius), 180f, color, duration, depthTest);
		}

		#endregion

		#region Sphere

		[Conditional("UNITY_EDITOR")]
		public static void DrawSphere(Vector3 center, float radius) => DrawSphere(center, radius, Quaternion.identity, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphere(Vector3 center, float radius, Color color) => DrawSphere(center, radius, Quaternion.identity, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphere(Vector3 center, float radius, Color color, float duration) => DrawSphere(center, radius, Quaternion.identity, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphere(Vector3 center, float radius, Color color, float duration, bool depthTest) => DrawSphere(center, radius, Quaternion.identity, color, duration, depthTest);
		[Conditional("UNITY_EDITOR")]
		private static void DrawSphere(Vector3 center, float radius, Quaternion orientation, Color color, float duration, bool depthTest) {
			DrawArc(center, orientation * Vector3.up, center + orientation * new Vector3(radius, 0f, 0f), 360f, color, duration, depthTest);
			DrawArc(center, orientation * Vector3.forward, center + orientation * new Vector3(radius, 0f, 0f), 360f, color, duration, depthTest);
			DrawArc(center, orientation * Vector3.right, center + orientation * new Vector3(0f, 0f, radius), 360f, color, duration, depthTest);
		}

		#endregion

		#region Capsule

		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 p1, Vector3 p2, float radius) => DrawCapsule(p1, p2, radius, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 p1, Vector3 p2, float radius, Color color) => DrawCapsule(p1, p2, radius, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 p1, Vector3 p2, float radius, Color color, float duration) => DrawCapsule(p1, p2, radius, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsule(Vector3 p1, Vector3 p2, float radius, Color color, float duration, bool depthTest) {
			Quaternion orientationP1 = Quaternion.identity;
			Quaternion orientationP2 = new Quaternion(0f, 0f, 1f, 0f);

			if (p1 != p2) {
				orientationP1 = Quaternion.FromToRotation(Vector3.up, p1 - p2);
				orientationP2 = Quaternion.FromToRotation(Vector3.up, p2 - p1);
			}

			Vector3 body = p2 - p1;
			float height = body.magnitude;
			Vector3 down = body / height;

			DrawHemisphere(p1, radius, orientationP1, color, duration, depthTest);

			Debug.DrawRay(p1 + orientationP1 * new Vector3(radius, 0f, 0f), down * height, color, duration, depthTest);
			Debug.DrawRay(p1 + orientationP1 * new Vector3(-radius, 0f, 0f), down * height, color, duration, depthTest);
			Debug.DrawRay(p1 + orientationP1 * new Vector3(0f, 0f, radius), down * height, color, duration, depthTest);
			Debug.DrawRay(p1 + orientationP1 * new Vector3(0f, 0f, -radius), down * height, color, duration, depthTest);

			DrawHemisphere(p2, radius, orientationP2, color, duration, depthTest);
		}

		#endregion

		#region Arc

		[Conditional("UNITY_EDITOR")]
		public static void DrawArc(Vector3 center, Vector3 normal, Vector3 from, float angle) => DrawArc(center, normal, from, angle, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawArc(Vector3 center, Vector3 normal, Vector3 from, float angle, Color color) => DrawArc(center, normal, from, angle, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawArc(Vector3 center, Vector3 normal, Vector3 from, float angle, Color color, float duration) => DrawArc(center, normal, from, angle, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawArc(Vector3 center, Vector3 normal, Vector3 from, float angle, Color color, float duration, bool depthTest) {
			const float ANGLES_PER_POINT = 10f;

			s_points ??= new List<Vector3>();
			s_points.Clear();

			Vector3 pointOffset = from - center;
			int pointCount = Mathf.CeilToInt(angle / ANGLES_PER_POINT);
			Quaternion stepRotation = Quaternion.AngleAxis(angle / pointCount, normal);

			s_points.Add(from);
			for (int i = 0; i < pointCount; i++) {
				pointOffset = stepRotation * pointOffset;
				s_points.Add(center + pointOffset);
			}

			DrawPolyLine(s_points, color, duration, depthTest);
		}

		#endregion

		#region BoxCast

		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, float maxDistance) => DrawBoxCast(center, extents, direction, Quaternion.identity, maxDistance, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, float maxDistance, Color color) => DrawBoxCast(center, extents, direction, Quaternion.identity, maxDistance, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, float maxDistance, Color color, float duration) => DrawBoxCast(center, extents, direction, Quaternion.identity, maxDistance, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, float maxDistance, Color color, float duration, bool depthTest) => DrawBoxCast(center, extents, direction, Quaternion.identity, maxDistance, color, duration, depthTest);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, Quaternion orientation, float maxDistance) => DrawBoxCast(center, extents, direction, orientation, maxDistance, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, Quaternion orientation, float maxDistance, Color color) => DrawBoxCast(center, extents, direction, orientation, maxDistance, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, Quaternion orientation, float maxDistance, Color color, float duration) => DrawBoxCast(center, extents, direction, orientation, maxDistance, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawBoxCast(Vector3 center, Vector3 extents, Vector3 direction, Quaternion orientation, float maxDistance, Color color, float duration, bool depthTest) {
			direction.Normalize();

			Vector3 size = extents * 2f;

			DrawCube(center, size, orientation, color, duration, depthTest);

			Vector3 bottomCloseLeft = center - orientation * extents;
			Vector3 bottomCloseRight = bottomCloseLeft + orientation * new Vector3(size.x, 0f, 0f);

			Vector3 topCloseLeft = bottomCloseLeft + orientation * new Vector3(0f, size.y, 0f);
			Vector3 topCloseRight = bottomCloseRight + orientation * new Vector3(0f, size.y, 0f);

			Vector3 topFarRight = center + orientation * extents;
			Vector3 topFarLeft = topFarRight - orientation * new Vector3(size.x, 0f, 0f);

			Vector3 bottomFarLeft = topFarLeft - orientation * new Vector3(0f, size.y, 0f);
			Vector3 bottomFarRight = topFarRight - orientation * new Vector3(0f, size.y, 0f);

			Vector3 otherOffset = direction * maxDistance;

			Debug.DrawRay(bottomCloseLeft, otherOffset, color, duration, depthTest);
			Debug.DrawRay(bottomCloseRight, otherOffset, color, duration, depthTest);
			Debug.DrawRay(topCloseLeft, otherOffset, color, duration, depthTest);
			Debug.DrawRay(topCloseRight, otherOffset, color, duration, depthTest);
			Debug.DrawRay(topFarRight, otherOffset, color, duration, depthTest);
			Debug.DrawRay(topFarLeft, otherOffset, color, duration, depthTest);
			Debug.DrawRay(bottomFarLeft, otherOffset, color, duration, depthTest);
			Debug.DrawRay(bottomFarRight, otherOffset, color, duration, depthTest);

			Vector3 otherCenter = center + otherOffset;

			DrawCube(otherCenter, size, orientation, color, duration, depthTest);
		}

		#endregion

		#region SphereCast

		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Ray ray, float radius, float maxDistance) => DrawSphereCast(ray.origin, radius, ray.direction, maxDistance, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Ray ray, float radius, float maxDistance, Color color) => DrawSphereCast(ray.origin, radius, ray.direction, maxDistance, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Ray ray, float radius, float maxDistance, Color color, float duration) => DrawSphereCast(ray.origin, radius, ray.direction, maxDistance, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Ray ray, float radius, float maxDistance, Color color, float duration, bool depthTest) => DrawSphereCast(ray.origin, radius, ray.direction, maxDistance, color, duration, depthTest);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance) => DrawSphereCast(origin, radius, direction, maxDistance, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, Color color) => DrawSphereCast(origin, radius, direction, maxDistance, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, Color color, float duration) => DrawSphereCast(origin, radius, direction, maxDistance, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawSphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, Color color, float duration, bool depthTest) {
			direction.Normalize();

			Quaternion rot = direction != Vector3.zero ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity;

			DrawSphere(origin, radius, rot, color, duration, depthTest);

			Vector3 otherOrigin = origin + direction * maxDistance;

			Vector3 rightVertexOffset = rot * new Vector3(radius, 0f, 0f);
			Vector3 leftVertexOffset = rot * new Vector3(-radius, 0f, 0f);
			Vector3 upVertexOffset = rot * new Vector3(0f, radius, 0f);
			Vector3 downVertexOffset = rot * new Vector3(0f, -radius, 0f);

			Debug.DrawLine(origin + rightVertexOffset, otherOrigin + rightVertexOffset, color, duration, depthTest);
			Debug.DrawLine(origin + leftVertexOffset, otherOrigin + leftVertexOffset, color, duration, depthTest);
			Debug.DrawLine(origin + upVertexOffset, otherOrigin + upVertexOffset, color, duration, depthTest);
			Debug.DrawLine(origin + downVertexOffset, otherOrigin + downVertexOffset, color, duration, depthTest);

			DrawSphere(otherOrigin, radius, rot, color, duration, depthTest);
		}

		#endregion

		#region CapsuleCast

		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsuleCast(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float maxDistance) => DrawCapsuleCast(p1, p2, radius, direction, maxDistance, Color.white, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsuleCast(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float maxDistance, Color color) => DrawCapsuleCast(p1, p2, radius, direction, maxDistance, color, 0f, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsuleCast(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float maxDistance, Color color, float duration) => DrawCapsuleCast(p1, p2, radius, direction, maxDistance, color, duration, true);
		[Conditional("UNITY_EDITOR")]
		public static void DrawCapsuleCast(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float maxDistance, Color color, float duration, bool depthTest) {
			direction.Normalize();

			DrawCapsule(p1, p2, radius, color, duration, depthTest);

			Vector3 otherOffset = direction * maxDistance;
			Vector3 otherP1 = p1 + otherOffset;
			Vector3 otherP2 = p2 + otherOffset;

			Quaternion orientationP1 = Quaternion.identity;
			Quaternion orientationP2 = new Quaternion(0f, 0f, 1f, 0f);

			if (p1 != p2) {
				orientationP1 = Quaternion.FromToRotation(Vector3.up, p1 - p2);
				orientationP2 = Quaternion.FromToRotation(Vector3.up, p2 - p1);
			}
			Vector3 rightVertexOffsetP1 = orientationP1 * new Vector3(radius, 0f, 0f);
			Vector3 leftVertexOffsetP1 = orientationP1 * new Vector3(-radius, 0f, 0f);
			Vector3 upVertexOffsetP1 = orientationP1 * new Vector3(0f, radius, 0f);

			Debug.DrawRay(p1 + rightVertexOffsetP1, otherOffset, color, duration, depthTest);
			Debug.DrawRay(p1 + leftVertexOffsetP1, otherOffset, color, duration, depthTest);
			Debug.DrawRay(p1 + upVertexOffsetP1, otherOffset, color, duration, depthTest);

			Vector3 rightVertexOffsetP2 = orientationP2 * new Vector3(radius, 0f, 0f);
			Vector3 leftVertexOffsetP2 = orientationP2 * new Vector3(-radius, 0f, 0f);
			Vector3 upVertexOffsetP2 = orientationP2 * new Vector3(0f, radius, 0f);

			Debug.DrawRay(p2 + rightVertexOffsetP2, otherOffset, color, duration, depthTest);
			Debug.DrawRay(p2 + leftVertexOffsetP2, otherOffset, color, duration, depthTest);
			Debug.DrawRay(p2 + upVertexOffsetP2, otherOffset, color, duration, depthTest);

			DrawCapsule(otherP1, otherP2, radius, color, duration, depthTest);
		}

		#endregion

		[Conditional("UNITY_EDITOR")]
		private static void DrawPolyLine(IList<Vector3> points, Color color, float duration, bool depthTest) {
			for (int i = 0; i < points.Count - 1; i++) {
				Debug.DrawLine(points[i], points[i + 1], color, duration, depthTest);
			}
		}
	}
}
