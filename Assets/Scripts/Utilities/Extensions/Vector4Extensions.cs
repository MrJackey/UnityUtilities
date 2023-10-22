using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class Vector4Extensions {
		/// <summary>
		/// Returns a Vector4 copy giving x a new value
		/// </summary>
		public static Vector4 WithX(this Vector4 vec, float x) => new Vector4(x, vec.y, vec.z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving y a new value
		/// </summary>
		public static Vector4 WithY(this Vector4 vec, float y) => new Vector4(vec.x, y, vec.z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving z a new value
		/// </summary>
		public static Vector4 WithZ(this Vector4 vec, float z) => new Vector4(vec.x, vec.y, z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving w a new value
		/// </summary>
		public static Vector4 WithW(this Vector4 vec, float w) => new Vector4(vec.x, vec.y, vec.z, w);

		/// <summary>
		/// Returns a Vector4 copy giving x and y new values
		/// </summary>
		public static Vector4 WithXY(this Vector4 vec, float x, float y) => new Vector4(x, y, vec.z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving x and z new values
		/// </summary>
		public static Vector4 WithXZ(this Vector4 vec, float x, float z) => new Vector4(x, vec.y, z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving x and w new values
		/// </summary>
		public static Vector4 WithXW(this Vector4 vec, float x, float w) => new Vector4(x, vec.y, vec.z, w);

		/// <summary>
		/// Returns a Vector4 copy giving y and z new values
		/// </summary>
		public static Vector4 WithYZ(this Vector4 vec, float y, float z) => new Vector4(vec.x, y, z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving y and w new values
		/// </summary>
		public static Vector4 WithYW(this Vector4 vec, float y, float w) => new Vector4(vec.x, y, vec.z, w);

		/// <summary>
		/// Returns a Vector4 copy giving z and w new values
		/// </summary>
		public static Vector4 WithZW(this Vector4 vec, float z, float w) => new Vector4(vec.x, vec.y, z, w);

		/// <summary>
		/// Returns a Vector4 copy giving x, y and z new values
		/// </summary>
		public static Vector4 WithXYZ(this Vector4 vec, float x, float y, float z) => new Vector4(x, y, z, vec.w);

		/// <summary>
		/// Returns a Vector4 copy giving x, y and w new values
		/// </summary>
		public static Vector4 WithXYW(this Vector4 vec, float x, float y, float w) => new Vector4(x, y, vec.z, w);

		/// <summary>
		/// Returns a Vector4 copy giving x, z and w new values
		/// </summary>
		public static Vector4 WithXZW(this Vector4 vec, float x, float z, float w) => new Vector4(x, vec.y, z, w);

		/// <summary>
		/// Returns a Vector4 copy giving y, z and w new values
		/// </summary>
		public static Vector4 WithYZW(this Vector4 vec, float y, float z, float w) => new Vector4(vec.x, y, z, w);
	}
}
