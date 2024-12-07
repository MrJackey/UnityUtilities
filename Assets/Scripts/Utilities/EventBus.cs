using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jackey.Utilities {
	/// <summary>
	/// Interface for sending globally accessed events. It is especially useful for
	/// many -> many events which can be hard to properly subscribe to and unsubscribe from.
	/// </summary>
	public static class EventBus {
		private static readonly Dictionary<Type, List<(MethodInfo AddListener, MethodInfo RemoveListener)>> s_listenerBusCache = new();
		private static readonly object[] s_singleListenerArray = new object[1];

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

			private static bool s_isDirty = true;

			private static readonly List<IEventBusListener<T>> s_invokeListeners = new();
			private static readonly List<EventBusCallback<T>> s_invokeCallbacks = new();

			public static void AddListener(IEventBusListener<T> listener) {
				s_listeners.Add(listener);
				s_isDirty = true;
			}

			public static void AddCallback(EventBusCallback<T> callback) {
				s_callbacks.Add(callback);
				s_isDirty = true;
			}

			public static void RemoveListener(IEventBusListener<T> listener) {
				s_listeners.Remove(listener);
				s_isDirty = true;
			}

			public static void RemoveCallback(EventBusCallback<T> callback) {
				s_callbacks.Remove(callback);
				s_isDirty = true;
			}

			public static void Invoke(T args) {
				if (s_isDirty) {
					s_invokeListeners.Clear();
					s_invokeListeners.AddRange(s_listeners);

					s_invokeCallbacks.Clear();
					s_invokeCallbacks.AddRange(s_callbacks);

					s_isDirty = false;
				}

				int listenerCount = s_invokeListeners.Count;
				for (int i = 0; i < listenerCount; i++) {
					s_invokeListeners[i].OnEvent(args);
				}

				int callbackCount = s_invokeCallbacks.Count;
				for (int i = 0; i < callbackCount; i++) {
					s_invokeCallbacks[i].Invoke(args);
				}
			}

			public static void InvokeSafe(T args) {
				if (s_isDirty) {
					s_invokeListeners.Clear();
					s_invokeListeners.AddRange(s_listeners);

					s_invokeCallbacks.Clear();
					s_invokeCallbacks.AddRange(s_callbacks);

					s_isDirty = false;
				}

				List<Exception> exceptions = null;

				int listenerCount = s_invokeListeners.Count;
				for (int i = 0; i < listenerCount; i++) {
					try {
						s_invokeListeners[i].OnEvent(args);
					}
					catch (Exception e) {
						exceptions ??= new List<Exception>();
						exceptions.Add(e);
					}
				}

				int callbackCount = s_invokeCallbacks.Count;
				for (int i = 0; i < callbackCount; i++) {
					try {
						s_invokeCallbacks[i].Invoke(args);
					}
					catch (Exception e) {
						exceptions ??= new List<Exception>();
						exceptions.Add(e);
					}
				}

				if (exceptions != null)
					throw new AggregateException("EventBus invocation threw exceptions on one or more subscribers", exceptions);
			}
		}
	}

	public delegate void EventBusCallback<T>(T args);

	public interface IEvent { }
	public interface IEventBusListener { }
	public interface IEventBusListener<T> : IEventBusListener where T : IEvent {
		public void OnEvent(T args);
	}
}
