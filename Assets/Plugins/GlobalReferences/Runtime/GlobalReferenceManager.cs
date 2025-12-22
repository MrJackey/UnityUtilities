using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jackey.GlobalReferences.Utilities;
using UnityEngine;

namespace Jackey.GlobalReferences {
	public static class GlobalReferenceManager {
		private static readonly Dictionary<SerializedGUID, GlobalId> s_instances = new();

#if UNITY_EDITOR
		internal static List<GlobalId> EditMode_InstanceList = new();
		internal static event Action EditModeListUpdated;
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize() {
			s_instances.Clear();
#if UNITY_EDITOR
			EditMode_InstanceList.Clear();
#endif
		}

		internal static void AddInstance(GlobalId instance) {
#if UNITY_EDITOR
			AddListInstance(instance);
			if (!Application.isPlaying)
				return;
#endif

			if (!s_instances.TryAdd(instance.GUID, instance)) {
				Debug.LogWarning($"[Global References] Multiple instances with the same id \"{instance.GUID}\" - Registered Instance: \"{s_instances[instance.GUID].name}\"", s_instances[instance.GUID]);
				Debug.LogWarning($"[Global References] Other instance \"{instance.name}\"", instance);
			}
		}

		internal static void RemoveInstance(GlobalId instance) {
#if UNITY_EDITOR
			RemoveListInstance(instance);
			if (!Application.isPlaying)
				return;
#endif

			s_instances.Remove(instance.GUID);
		}

		internal static void RemoveInstance(GlobalId instance, SerializedGUID guid) {
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				RemoveListInstance(instance);
				return;
			}
#endif

			s_instances.Remove(guid);
		}

		#region Resolve

		public static GameObject Resolve(GlobalRef reference) => Resolve(reference.GUID);
		public static GameObject Resolve(SerializedGUID guid) {
			if (TryGetInstance(guid, out GlobalId instance))
				return instance.GameObject;

			return null;
		}

		public static T Resolve<T>(GlobalRef reference) => Resolve<T>(reference.GUID);
		public static T Resolve<T>(SerializedGUID guid) {
			if (TryGetInstance(guid, out GlobalId instance))
				return instance.GameObject.GetComponent<T>();

			return default;
		}

		public static T ResolveInParent<T>(GlobalRef reference) => ResolveInParent<T>(reference.GUID);
		public static T ResolveInParent<T>(SerializedGUID guid) {
			if (TryGetInstance(guid, out GlobalId instance))
				return instance.GameObject.GetComponentInParent<T>();

			return default;
		}

		public static T ResolveInChildren<T>(GlobalRef reference) => ResolveInChildren<T>(reference.GUID);
		public static T ResolveInChildren<T>(SerializedGUID guid) {
			if (TryGetInstance(guid, out GlobalId instance))
				return instance.GameObject.GetComponentInChildren<T>();

			return default;
		}

		public static bool TryResolve(GlobalRef reference, out GameObject gameObject) => TryResolve(reference.GUID, out gameObject);
		public static bool TryResolve(SerializedGUID guid, out GameObject gameObject) {
			if (TryGetInstance(guid, out GlobalId instance)) {
				gameObject = instance.GameObject;
				return true;
			}

			gameObject = null;
			return false;
		}

		public static bool TryResolve<T>(GlobalRef reference, out T component) => TryResolve(reference.GUID, out component);
		public static bool TryResolve<T>(SerializedGUID guid, out T component) {
			if (TryGetInstance(guid, out GlobalId instance))
				return instance.GameObject.TryGetComponent(out component);

			component = default;
			return false;
		}

		public static bool TryResolveInParent<T>(GlobalRef reference, out T component) => TryResolveInParent<T>(reference.GUID, out component);
		public static bool TryResolveInParent<T>(SerializedGUID guid, out T component) {
			if (TryGetInstance(guid, out GlobalId instance)) {
				component = instance.GameObject.GetComponentInParent<T>();
				return component != null;
			}

			component = default;
			return false;
		}

		public static bool TryResolveInChildren<T>(GlobalRef reference, out T component) => TryResolveInChildren<T>(reference.GUID, out component);
		public static bool TryResolveInChildren<T>(SerializedGUID guid, out T component) {
			if (TryGetInstance(guid, out GlobalId instance)) {
				component = instance.GameObject.GetComponentInChildren<T>();
				return component != null;
			}

			component = default;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool TryGetInstance(SerializedGUID guid, out GlobalId instance) {
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return TryEditModeResolve(guid, out instance);
#endif

			return s_instances.TryGetValue(guid, out instance);
		}

#if UNITY_EDITOR
		private static bool TryEditModeResolve(SerializedGUID guid, out GlobalId instance) {
			foreach (GlobalId id in EditMode_InstanceList) {
				if (id.GUID == guid) {
					instance = id;
					return true;
				}
			}

			instance = null;
			return false;
		}
#endif

		#endregion

#if UNITY_EDITOR
		internal static void MoveEditModeInstance(GlobalId instance, SerializedGUID from) {
			s_instances.Remove(from);
			s_instances.TryAdd(instance.GUID, instance);

			EditMode_InstanceList.Remove(instance);
			AddListInstance(instance);
		}

		private static void AddListInstance(GlobalId instance) {
			int index = EditMode_InstanceList.FindIndex(list => list.GUID == instance.GUID);
			if (index == -1)
				EditMode_InstanceList.Add(instance);
			else
				EditMode_InstanceList.Insert(index + 1, instance);

			EditModeListUpdated?.Invoke();
		}

		private static void RemoveListInstance(GlobalId instance) {
			EditMode_InstanceList.Remove(instance);
			EditModeListUpdated?.Invoke();
		}
#endif
	}
}
