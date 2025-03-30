using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jackey.EventBus {
	/// <summary>
	/// Interface for sending globally accessed events. It is especially useful for
	/// many -> many events which can be hard to properly subscribe to and unsubscribe from.
	/// </summary>
	public static class EventBus {
		private static readonly Dictionary<Type, List<(MethodInfo AddListener, MethodInfo RemoveListener)>> s_listenerBusCache = new();
		private static readonly object[] s_singleListenerArray = new object[1];

#if UNITY_EDITOR
		internal static List<Type> Editor_Buses { get; } = new();
		internal static event Action Editor_BusConstructed;
		internal static event Action<Type> Editor_BusUpdated;
#endif

		/// <summary>
		/// Subscribe a listener to an event
		/// </summary>
		/// <param name="listener">The listener to receive the callback</param>
		/// <typeparam name="T">The signature of the event</typeparam>
		public static void Subscribe<T>(IEventBusListener<T> listener) where T : IEvent {
			Bus<T>.AddListener(listener);
		}

		/// <summary>
		/// Subscribe a callback to an event
		/// </summary>
		/// <param name="callback">The callback to be run on event invocation</param>
		/// <typeparam name="T">The signature of the event</typeparam>
		public static void Subscribe<T>(EventBusCallback<T> callback) where T : IEvent {
			Bus<T>.AddCallback(callback);
		}

		/// <summary>
		/// Unsubscribe a listener from an event
		/// </summary>
		/// <param name="listener">The listener receiving the callback</param>
		/// <typeparam name="T">The signature of the event</typeparam>
		public static void Unsubscribe<T>(IEventBusListener<T> listener) where T : IEvent {
			Bus<T>.RemoveListener(listener);
		}

		/// <summary>
		/// Unsubscribe a callback from an event
		/// </summary>
		/// <param name="callback">The callback running on event invocations</param>
		/// <typeparam name="T">The signature of the event</typeparam>
		public static void Unsubscribe<T>(EventBusCallback<T> callback) where T : IEvent {
			Bus<T>.RemoveCallback(callback);
		}

		/// <summary>
		/// Subscribe a listener to all of its events
		/// </summary>
		/// <param name="listener">The listener to receive the callbacks</param>
		public static void SubscribeAll(IEventBusListener listener) {
			s_singleListenerArray[0] = listener;

			foreach ((MethodInfo AddListener, MethodInfo _) bus in GetAllListenerBuses(listener)) {
				bus.AddListener.Invoke(null, s_singleListenerArray);
			}

			s_singleListenerArray[0] = null;
		}

		/// <inheritdoc cref="SubscribeAll"/>
		public static void SubscribeAll<T>(IEventBusListener<T> listener) where T : IEvent {
			Bus<T>.AddListener(listener);
		}

		/// <summary>
		/// Unsubscribe a listener from all of its events
		/// </summary>
		/// <param name="listener">The listener receiving the callbacks</param>
		public static void UnsubscribeAll(IEventBusListener listener) {
			s_singleListenerArray[0] = listener;

			foreach ((MethodInfo _, MethodInfo RemoveListener) bus in GetAllListenerBuses(listener)) {
				bus.RemoveListener.Invoke(null, s_singleListenerArray);
			}

			s_singleListenerArray[0] = null;
		}

		/// <inheritdoc cref="UnsubscribeAll"/>
		public static void UnsubscribeAll<T>(IEventBusListener<T> listener) where T : IEvent {
			Bus<T>.RemoveListener(listener);
		}

		/// <summary>
		/// Invoke an event using default values
		/// </summary>
		/// <typeparam name="T">The signature of the event</typeparam>
		public static void Invoke<T>() where T : IEvent {
			Bus<T>.Invoke(default);
		}

		/// <summary>
		/// Invoke an event with custom values
		/// </summary>
		/// <param name="args">The values to pass into the event</param>
		/// <typeparam name="T">The signature of the event</typeparam>
		public static void Invoke<T>(T args) where T : IEvent {
			Bus<T>.Invoke(args);
		}

		/// <summary>
		/// Invoke an event using default values. If an exception occurs the invocation continues
		/// as normal
		/// </summary>
		/// <typeparam name="T">The signature of the event</typeparam>
		/// <exception cref="AggregateException">One or more listeners threw an exception. Thrown after all listeners are done</exception>
		public static void InvokeSafe<T>() where T : IEvent {
			Bus<T>.InvokeSafe(default);
		}

		/// <summary>
		/// Invoke an event with custom values. If an exception occurs the invocation continues
		/// as normal
		/// </summary>
		/// <param name="args">The values to pass into the event</param>
		/// <typeparam name="T">The signature of the event</typeparam>
		/// <exception cref="AggregateException">One or more listeners threw an exception. Thrown after all listeners are done</exception>
		public static void InvokeSafe<T>(T args) where T : IEvent {
			Bus<T>.InvokeSafe(args);
		}

		/// <summary>
		/// Clear the cache populated by using the SubscribeAll() and UnsubscribeAll() subscribe methods
		/// </summary>
		public static void ClearCache() {
			s_listenerBusCache.Clear();
		}

		private static List<(MethodInfo AddListener, MethodInfo RemoveListener)> GetAllListenerBuses(IEventBusListener listener) {
			Type listenerType = listener.GetType();

			if (s_listenerBusCache.TryGetValue(listenerType, out List<(MethodInfo, MethodInfo)> buses))
				return buses;

			buses = new List<(MethodInfo, MethodInfo)>();

			foreach (Type implementation in listenerType.GetInterfaces()) {
				if (implementation.IsGenericType) {
					Type typeDef = implementation.GetGenericTypeDefinition();

					if (typeDef == typeof(IEventBusListener<>)) {
						Type busType = typeof(Bus<>);

						foreach (Type evt in implementation.GetGenericArguments()) {
							Type genericBusType = busType.MakeGenericType(evt);

							buses.Add((
								genericBusType.GetMethod("AddListener"),
								genericBusType.GetMethod("RemoveListener")
							));
						}
					}
				}
			}

			s_listenerBusCache.Add(listenerType, buses);
			return buses;
		}

		private static class Bus<T> where T : IEvent {
			private static readonly List<IEventBusListener<T>> s_listeners = new();
			private static readonly List<EventBusCallback<T>> s_callbacks = new();
			private static readonly List<IEventBusListener<T>> s_deferredListeners = new();
			private static readonly List<EventBusCallback<T>> s_deferredCallbacks = new();

			private static bool s_invoking;
			private static int s_listenerInvokeIndex = -1;
			private static int s_callbackInvokeIndex = -1;

#if UNITY_EDITOR
			static Bus() {
				Editor_Buses.Add(typeof(Bus<T>));
				Editor_BusConstructed?.Invoke();
			}
#endif

			public static void AddListener(IEventBusListener<T> listener) {
				if (s_invoking)
					s_deferredListeners.Add(listener);
				else
					s_listeners.Add(listener);

#if UNITY_EDITOR
				Editor_InvokeUpdate();
#endif
			}

			public static void AddCallback(EventBusCallback<T> callback) {
				if (s_invoking)
					s_deferredCallbacks.Add(callback);
				else
					s_callbacks.Add(callback);

#if UNITY_EDITOR
				Editor_InvokeUpdate();
#endif
			}

			public static void RemoveListener(IEventBusListener<T> listener) {
				if (!s_invoking) {
					s_listeners.Remove(listener);

#if UNITY_EDITOR
					Editor_InvokeUpdate();
#endif
					return;
				}

				s_deferredListeners.Remove(listener);

				int listenerIndex = s_listeners.IndexOf(listener);
				if (listenerIndex == -1)
					return;

				if (listenerIndex <= s_listenerInvokeIndex)
					s_listenerInvokeIndex--;

				s_listeners.RemoveAt(listenerIndex);

#if UNITY_EDITOR
				Editor_InvokeUpdate();
#endif
			}

			public static void RemoveCallback(EventBusCallback<T> callback) {
				if (!s_invoking) {
					s_callbacks.Remove(callback);

#if UNITY_EDITOR
					Editor_InvokeUpdate();
#endif
					return;
				}

				s_deferredCallbacks.Remove(callback);

				int callbackIndex = s_callbacks.IndexOf(callback);
				if (callbackIndex == -1)
					return;

				if (callbackIndex <= s_callbackInvokeIndex)
					s_callbackInvokeIndex--;

				s_callbacks.RemoveAt(callbackIndex);

#if UNITY_EDITOR
				Editor_InvokeUpdate();
#endif
			}

			public static void Invoke(T args) {
				s_invoking = true;

				try {
					for (s_listenerInvokeIndex = 0; s_listenerInvokeIndex < s_listeners.Count; s_listenerInvokeIndex++) {
						s_listeners[s_listenerInvokeIndex].OnEvent(args);
					}
					s_listenerInvokeIndex = -1;

					for (s_callbackInvokeIndex = 0; s_callbackInvokeIndex < s_callbacks.Count; s_callbackInvokeIndex++) {
						s_callbacks[s_callbackInvokeIndex].Invoke(args);
					}
					s_callbackInvokeIndex = -1;
				}
				finally {
					if (s_deferredListeners.Count > 0) {
						for (int i = 0; i < s_deferredListeners.Count; i++)
							s_listeners.Add(s_deferredListeners[i]);

						s_deferredListeners.Clear();

#if UNITY_EDITOR
						Editor_InvokeUpdate();
#endif
					}

					if (s_deferredCallbacks.Count > 0) {
						for (int i = 0; i < s_deferredCallbacks.Count; i++)
							s_callbacks.Add(s_deferredCallbacks[i]);

						s_deferredCallbacks.Clear();

#if UNITY_EDITOR
						Editor_InvokeUpdate();
#endif
					}

					s_invoking = false;
				}
			}

			public static void InvokeSafe(T args) {
				List<Exception> exceptions = null;

				for (s_listenerInvokeIndex = 0; s_listenerInvokeIndex < s_listeners.Count; s_listenerInvokeIndex++) {
					try {
						s_listeners[s_listenerInvokeIndex].OnEvent(args);
					}
					catch (Exception e) {
						exceptions ??= new List<Exception>();
						exceptions.Add(e);
					}
				}
				s_listenerInvokeIndex = -1;

				for (s_callbackInvokeIndex = 0; s_callbackInvokeIndex < s_callbacks.Count; s_callbackInvokeIndex++) {
					try {
						s_callbacks[s_callbackInvokeIndex].Invoke(args);
					}
					catch (Exception e) {
						exceptions ??= new List<Exception>();
						exceptions.Add(e);
					}
				}
				s_callbackInvokeIndex = -1;

				if (s_deferredListeners.Count > 0) {
					for (int i = 0; i < s_deferredListeners.Count; i++)
						s_listeners.Add(s_deferredListeners[i]);

					s_deferredListeners.Clear();

#if UNITY_EDITOR
					Editor_InvokeUpdate();
#endif
				}

				if (s_deferredCallbacks.Count > 0) {
					for (int i = 0; i < s_deferredCallbacks.Count; i++)
						s_callbacks.Add(s_deferredCallbacks[i]);

					s_deferredCallbacks.Clear();

#if UNITY_EDITOR
					Editor_InvokeUpdate();
#endif
				}

				if (exceptions != null)
					throw new AggregateException("EventBus invocation threw exceptions on one or more subscribers", exceptions);
			}

#if UNITY_EDITOR
			private static void Editor_InvokeUpdate() {
				Editor_BusUpdated?.Invoke(typeof(Bus<T>));
			}
#endif
		}
	}

	public delegate void EventBusCallback<T>(T args);

	public interface IEvent { }
	public interface IEventBusListener { }
	public interface IEventBusListener<T> : IEventBusListener where T : IEvent {
		public void OnEvent(T args);
	}
}
