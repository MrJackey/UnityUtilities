using System;
using System.Collections.Generic;
using Jackey.GlobalReferences.Utilities;
using UnityEngine;

namespace Jackey.GlobalReferences {
	public static class GlobalReferenceManager {
		private static readonly Dictionary<SerializedGUID, GlobalId> s_instances = new();

#if UNITY_EDITOR
		internal static List<GlobalId> InstanceList = new();
		internal static event Action ListUpdated;
#endif

		internal static void AddInstance(GlobalId instance) {
			if (!s_instances.TryAdd(instance.GUID, instance))
				Debug.LogWarning($"[Global References] Multiple instances with the same id \"{instance.GUID}\"", instance);

#if UNITY_EDITOR
			AddListInstance(instance);
#endif
		}

		internal static void RemoveInstance(GlobalId instance) {
			s_instances.Remove(instance.GUID);

#if UNITY_EDITOR
			RemoveListInstance(instance);
#endif
		}

		internal static void RemoveInstance(GlobalId instance, SerializedGUID guid) {
			s_instances.Remove(guid);

#if UNITY_EDITOR
			RemoveListInstance(instance);
#endif
		}

		#region Resolve

		public static GameObject Resolve(GlobalRef reference) => Resolve(reference.GUID);
		public static GameObject Resolve(SerializedGUID guid) {
			if (s_instances.TryGetValue(guid, out GlobalId instance))
				return instance.GameObject;

			return null;
		}

		public static T Resolve<T>(GlobalRef reference) => Resolve<T>(reference.GUID);
		public static T Resolve<T>(SerializedGUID guid) {
			if (s_instances.TryGetValue(guid, out GlobalId instance))
				return instance.GameObject.GetComponent<T>();

			return default;
		}

		public static T ResolveInParent<T>(GlobalRef reference) => ResolveInParent<T>(reference.GUID);
		public static T ResolveInParent<T>(SerializedGUID guid) {
			if (s_instances.TryGetValue(guid, out GlobalId instance))
				return instance.GameObject.GetComponentInParent<T>();

			return default;
		}

		public static T ResolveInChildren<T>(GlobalRef reference) => ResolveInChildren<T>(reference.GUID);
		public static T ResolveInChildren<T>(SerializedGUID guid) {
			if (s_instances.TryGetValue(guid, out GlobalId instance))
				return instance.GameObject.GetComponentInChildren<T>();

			return default;
		}

		public static bool TryResolve(GlobalRef reference, out GameObject gameObject) => TryResolve(reference.GUID, out gameObject);
		public static bool TryResolve(SerializedGUID guid, out GameObject gameObject) {
			if (s_instances.TryGetValue(guid, out GlobalId instance)) {
				gameObject = instance.GameObject;
				return true;
			}

			gameObject = null;
			return false;
		}

		public static bool TryResolve<T>(GlobalRef reference, out T component) => TryResolve(reference.GUID, out component);
		public static bool TryResolve<T>(SerializedGUID guid, out T component) {
			if (s_instances.TryGetValue(guid, out GlobalId instance))
				return instance.GameObject.TryGetComponent(out component);

			component = default;
			return false;
		}

		public static bool TryResolveInParent<T>(GlobalRef reference, out T component) => TryResolveInParent<T>(reference.GUID, out component);
		public static bool TryResolveInParent<T>(SerializedGUID guid, out T component) {
			if (s_instances.TryGetValue(guid, out GlobalId instance)) {
				component = instance.GameObject.GetComponentInParent<T>();
				return component != null;
			}

			component = default;
			return false;
		}

		public static bool TryResolveInChildren<T>(GlobalRef reference, out T component) => TryResolveInChildren<T>(reference.GUID, out component);
		public static bool TryResolveInChildren<T>(SerializedGUID guid, out T component) {
			if (s_instances.TryGetValue(guid, out GlobalId instance)) {
				component = instance.GameObject.GetComponentInChildren<T>();
				return component != null;
			}

			component = default;
			return false;
		}

		#endregion

#if UNITY_EDITOR
		internal static void MoveEditModeInstance(GlobalId instance, SerializedGUID from) {
			s_instances.Remove(from);
			s_instances.TryAdd(instance.GUID, instance);

			InstanceList.Remove(instance);
			AddListInstance(instance);
		}

		private static void AddListInstance(GlobalId instance) {
			int index = InstanceList.FindIndex(list => list.GUID == instance.GUID);
			if (index == -1)
				InstanceList.Add(instance);
			else
				InstanceList.Insert(index + 1, instance);

			ListUpdated?.Invoke();
		}

		private static void RemoveListInstance(GlobalId instance) {
			InstanceList.Remove(instance);
			ListUpdated?.Invoke();
		}
#endif
	}
}
