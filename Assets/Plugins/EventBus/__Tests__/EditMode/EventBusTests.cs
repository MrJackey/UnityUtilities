using System;
using NUnit.Framework;

namespace Jackey.EventBus.__Tests__.EditMode {
	public class EventBusTests {
		[SetUp]
		public void SetUp() { }

		[TearDown]
		public void TearDown() {
			EventBus.ClearCache();
			EventBus.ClearBuses();
		}

		[Test]
		public void CanSubscribeListenerToEvent() {
			Event1Listener event1Listener = new();

			EventBus.Subscribe(event1Listener);
			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, event1Listener.Invocations);
		}


		[Test]
		public void CanSubscribeCallbackToEvent() {
			int invocations = 0;

			EventBus.Subscribe<Event1>(_ => invocations++);
			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, invocations);
		}


		[Test]
		public void CanUnsubscribeListenerFromEvent() {
			Event1Listener event1Listener = new();

			EventBus.Subscribe(event1Listener);
			EventBus.Invoke<Event1>();

			EventBus.Unsubscribe(event1Listener);
			EventBus.Invoke<Event1>();

			Assert.AreEqual(1,event1Listener.Invocations);
		}

		[Test]
		public void CanUnsubscribeCallbackFromEvent() {
			int invocations = 0;

			EventBusCallback<Event1> action = _ => invocations++;

			EventBus.Subscribe(action);
			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, invocations);

			EventBus.Unsubscribe(action);

			Assert.AreEqual(1, invocations);
		}

		[Test]
		public void InvokeOnlyNotifiesCorrectListeners() {
			Event1Listener event1Listener = new();
			Event2Listener event2Listener = new();

			EventBus.Subscribe(event1Listener);
			EventBus.Subscribe(event2Listener);

			EventBus.Invoke<Event1>();
			Assert.AreEqual(1, event1Listener.Invocations);
			Assert.AreEqual(0, event2Listener.Invocations);
		}

		[Test]
		public void ListenerCanSubscribeToMultipleEventsAtOnce() {
			Event1_2Listener event1_2Listener = new();

			EventBus.SubscribeAll(event1_2Listener);

			EventBus.Invoke<Event1>();
			EventBus.Invoke<Event2>();

			Assert.AreEqual(1, event1_2Listener.Invocations1);
			Assert.AreEqual(1, event1_2Listener.Invocations2);
		}

		[Test]
		public void ListenerCanUnsubscribeFromMultipleEventsAtOnce() {
			Event1_2Listener event1_2Listener = new();

			EventBus.SubscribeAll(event1_2Listener);

			EventBus.Invoke<Event1>();
			EventBus.Invoke<Event2>();

			EventBus.UnsubscribeAll(event1_2Listener);

			EventBus.Invoke<Event1>();
			EventBus.Invoke<Event2>();

			Assert.AreEqual(1, event1_2Listener.Invocations1);
			Assert.AreEqual(1, event1_2Listener.Invocations2);
		}

		[Test]
		public void ListenerCanBeUnsubscribedDuringInvocation() {
			Event1ListenerOnce event1Listener = new();

			EventBus.SubscribeAll(event1Listener);

			EventBus.Invoke<Event1>();
			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, event1Listener.Invocations);
		}

		[Test]
		public void CallbackCanBeUnsubscribedDuringInvocation() {
			int invocations = 0;

			void OnEvent(Event1 args) {
				invocations++;
				EventBus.Unsubscribe<Event1>(OnEvent);
			}

			EventBus.Subscribe<Event1>(OnEvent);
			EventBus.Subscribe<Event1>(OnEvent);

			EventBus.Invoke<Event1>();
			EventBus.Invoke<Event1>();
			EventBus.Invoke<Event1>();

			Assert.AreEqual(2, invocations);
		}

		[Test]
		public void ListenersToBeInvokedCanBeUnsubscribedDuringInvocation() {
			Event1Listener event1Listener = new Event1Listener();
			Event1Listener event1Listener2 = new Event1Listener();
			Event1ListenerRemoveOther event1ListenerRemoveOther = new Event1ListenerRemoveOther(event1Listener2);

			EventBus.Subscribe<Event1>(event1Listener);
			EventBus.Subscribe<Event1>(event1ListenerRemoveOther);
			EventBus.Subscribe<Event1>(event1Listener2);

			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, event1ListenerRemoveOther.Invocations);
			Assert.AreEqual(1, event1Listener.Invocations);
			Assert.AreEqual(0, event1Listener2.Invocations);
		}

		[Test]
		public void ListenersAlreadyInvokedCanBeUnsubscribedDuringInvocation() {
			Event1Listener event1Listener = new Event1Listener();
			Event1Listener event1Listener2 = new Event1Listener();
			Event1ListenerRemoveOther event1ListenerRemoveOther = new Event1ListenerRemoveOther(event1Listener);

			EventBus.Subscribe<Event1>(event1Listener);
			EventBus.Subscribe<Event1>(event1ListenerRemoveOther);
			EventBus.Subscribe<Event1>(event1Listener2);

			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, event1Listener.Invocations);
			Assert.AreEqual(1, event1ListenerRemoveOther.Invocations);
			Assert.AreEqual(1, event1Listener2.Invocations);

			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, event1Listener.Invocations);
			Assert.AreEqual(2, event1ListenerRemoveOther.Invocations);
			Assert.AreEqual(2, event1Listener2.Invocations);
		}

		[Test]
		public void CallbacksToBeInvokedCanBeUnsubscribedDuringInvocation() {
			int invocations = 0;
			int invocations2 = 0;

			EventBusCallback<Event1> callback1 = _ => invocations++;
			EventBusCallback<Event1> callback3 = _ => invocations++;
			EventBusCallback<Event1> callback2 = null;
			callback2 = _ => {
				invocations2++;
				EventBus.Unsubscribe(callback3);
			};

			EventBus.Subscribe<Event1>(callback1);
			EventBus.Subscribe<Event1>(callback2);
			EventBus.Subscribe<Event1>(callback3);

			EventBus.Invoke<Event1>();

			Assert.AreEqual(1, invocations);
			Assert.AreEqual(1, invocations2);
		}

		[Test]
		public void CallbacksAlreadyInvokedCanBeUnsubscribedDuringInvocation() {
			int invocations = 0;
			int invocations2 = 0;

			EventBusCallback<Event1> callback1 = _ => invocations++;
			EventBusCallback<Event1> callback2 = null;
			callback2 = _ => {
				invocations2++;
				EventBus.Unsubscribe(callback1);
			};
			EventBusCallback<Event1> callback3 = _ => invocations++;

			EventBus.Subscribe<Event1>(callback1);
			EventBus.Subscribe<Event1>(callback2);
			EventBus.Subscribe<Event1>(callback3);

			EventBus.Invoke<Event1>();

			Assert.AreEqual(2, invocations);
			Assert.AreEqual(1, invocations2);

			EventBus.Invoke<Event1>();

			Assert.AreEqual(3, invocations);
			Assert.AreEqual(2, invocations2);
		}

		[Test]
		public void SafeInvokeContinuesOnException() {
			int invocations = 0;
			EventBusCallback<Event1> event1Callback = _ => invocations++;

			Event1Listener event1Listener1 = new();
			Event1Error event1Error = new();
			Event1Listener event1Listener2 = new();

			EventBus.SubscribeAll(event1Listener1);
			EventBus.Subscribe(event1Callback);

			EventBus.SubscribeAll(event1Error);

			EventBus.Subscribe(event1Callback);
			EventBus.SubscribeAll(event1Listener2);

			Assert.Throws<AggregateException>(EventBus.InvokeSafe<Event1>);

			Assert.AreEqual(1, event1Listener1.Invocations);
			Assert.AreEqual(1, event1Listener2.Invocations);
			Assert.AreEqual(2, invocations);
		}
	}

	public class Event1Listener : IEventBusListener<Event1> {
		public int Invocations { get; private set; }

		void IEventBusListener<Event1>.OnEvent(Event1 args) => Invocations++;
	}

	public class Event2Listener : IEventBusListener<Event2> {
		public int Invocations { get; private set; }

		void IEventBusListener<Event2>.OnEvent(Event2 args) => Invocations++;
	}

	public class Event1_2Listener : IEventBusListener<Event1>, IEventBusListener<Event2> {
		public int Invocations1 { get; private set; }
		public int Invocations2 { get; private set; }

		void IEventBusListener<Event1>.OnEvent(Event1 args) => Invocations1++;
		void IEventBusListener<Event2>.OnEvent(Event2 args) => Invocations2++;
	}

	public class Event1ListenerOnce : IEventBusListener<Event1> {
		public int Invocations { get; private set; }

		void IEventBusListener<Event1>.OnEvent(Event1 args) {
			Invocations++;
			EventBus.UnsubscribeAll(this);
		}
	}

	public class Event1ListenerRemoveOther : IEventBusListener<Event1> {
		private IEventBusListener m_other;

		public int Invocations { get; private set; }

		public Event1ListenerRemoveOther(IEventBusListener other) {
			m_other = other;
		}

		void IEventBusListener<Event1>.OnEvent(Event1 args) {
			Invocations++;
			EventBus.UnsubscribeAll(m_other);
		}
	}

	public class Event1Error : IEventBusListener<Event1> {
		void IEventBusListener<Event1>.OnEvent(Event1 args) {
			throw new Exception();
		}
	}

	public struct Event1 : IEvent { }
	public struct Event2 : IEvent { }
}
