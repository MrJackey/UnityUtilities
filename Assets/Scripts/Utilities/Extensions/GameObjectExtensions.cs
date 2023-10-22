using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class GameObjectExtensions {
		/// <summary>
		/// Gets a component on the game object.
		/// If none is found, a new component is added and returned.
		/// </summary>
		/// <typeparam name="T">The type of the component to retrieve</typeparam>
		/// <returns>Returns the found component or a new one of none existed</returns>
		public static T GetOrAddComponent<T>(this GameObject source) where T : Component {
			if (source.TryGetComponent(out T component))
				return component;

			return source.AddComponent<T>();
		}

		/// <summary>
		/// Check if the game object has a component of a certain type
		/// </summary>
		/// <returns>Returns true if a component is found, otherwise false</returns>
		public static bool HasComponent<T>(this GameObject source) where T : Component {
			return source.TryGetComponent(out T _);
		}

		/// <summary>
		/// Tries to find a component on <paramref name="source"/> or any of its ancestors
		/// </summary>
		/// <returns>Returns true if a component is found, otherwise false</returns>
		public static bool TryGetComponentInParent<T>(this GameObject source, out T component, bool includeInactive = false) where T : Component {
			component = source.GetComponentInParent<T>(includeInactive);
			return component;
		}

		/// <summary>
		/// Tries to find a component on <paramref name="source"/> or any of its children
		/// </summary>
		/// <returns>Returns true if a component is found, otherwise false</returns>
		public static bool TryGetComponentInChildren<T>(this GameObject source, out T component, bool includeInactive = false) where T : Component {
			component = source.GetComponentInChildren<T>(includeInactive);
			return component;
		}
	}
}
