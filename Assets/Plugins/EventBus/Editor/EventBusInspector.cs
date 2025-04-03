using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.EventBus.Editor {
	public class EventBusInspector : EditorWindow {
		[SerializeField] private StyleSheet m_styleSheet;

		private ListView m_eventView;

		private ListView m_listenersView;
		private List<Clickable> m_listenerClickables = new();

		private ListView m_callbacksView;
		private List<Clickable> m_callbackClickables = new();

		[MenuItem("Tools/Jackey/EventBus/Inspector")]
		private static void ShowWindow() {
			EventBusInspector window = GetWindow<EventBusInspector>();
			window.Show();
		}

		private void OnEnable() {
			EventBus.Editor_BusConstructed += OnBusConstructed;
			EventBus.Editor_BusUpdated += OnBusUpdated;
		}

		private void OnDisable() {
			EventBus.Editor_BusConstructed -= OnBusConstructed;
			EventBus.Editor_BusUpdated -= OnBusUpdated;
		}

		private void CreateGUI() {
			titleContent = new GUIContent("EventBus Inspector");

			rootVisualElement.styleSheets.Add(m_styleSheet);

			TwoPaneSplitView splitView = new TwoPaneSplitView() {
				fixedPaneIndex = 0,
				fixedPaneInitialDimension = 250f,
				orientation = TwoPaneSplitViewOrientation.Horizontal,
			};
			rootVisualElement.Add(splitView);

			VisualElement eventsSection = new VisualElement();
			eventsSection.AddToClassList("Section");

			eventsSection.Add(new Label("Events"));
			eventsSection.Add(m_eventView = new ListView() {
				name = "EventList",
				itemsSource = EventBus.Editor_Buses,
				fixedItemHeight = 50,
				selectionType = SelectionType.Single,
				showBorder = false,
				reorderable = false,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				showBoundCollectionSize = true,
				showAddRemoveFooter = false,
				makeItem = MakeEventItem,
				bindItem = BindEventItem,
			});

			splitView.Add(eventsSection);

			m_eventView.selectionChanged += OnSelectedEventChanged;

			VisualElement subscriberSection = new VisualElement();
			subscriberSection.AddToClassList("Section");

			subscriberSection.Add(new Label("Listeners"));
			subscriberSection.Add(m_listenersView = new ListView() {
				itemsSource = null,
				selectionType = SelectionType.None,
				showBorder = false,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				showBoundCollectionSize = false,
				showAddRemoveFooter = false,
				makeItem = MakeListenerItem,
				bindItem = BindListenerItem,
				unbindItem = UnbindListenerItem,
			});

			subscriberSection.Add(new Label("Callbacks"));
			subscriberSection.Add(m_callbacksView = new ListView() {
				itemsSource = null,
				selectionType = SelectionType.None,
				showBorder = false,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				showBoundCollectionSize = false,
				showAddRemoveFooter = false,
				makeItem = MakeCallbackItem,
				bindItem = BindCallbackItem,
				unbindItem = UnbindCallbackItem,
			});

			splitView.Add(subscriberSection);
		}

		#region Event CRUD

		private VisualElement MakeEventItem() {
			VisualElement eventListItem = new VisualElement() { name = "EventListItem" };
			eventListItem.Add(new Label() { name = "EventName" });
			eventListItem.Add(new Label() { name = "ListenerCount" });
			eventListItem.Add(new Label() { name = "CallbackCount" });

			return eventListItem;
		}

		private void BindEventItem(VisualElement visualElement, int i) {
			visualElement.Q<Label>("EventName").text = EventBus.Editor_Buses[i].GenericTypeArguments[0].FullName;

			(IList listeners, IList callbacks) = GetBusSubscribers(i);
			visualElement.Q<Label>("ListenerCount").text = $"Listeners: {listeners.Count}";
			visualElement.Q<Label>("CallbackCount").text = $"Callbacks: {callbacks.Count}";
		}

		#endregion

		private void OnSelectedEventChanged(IEnumerable<object> _) {
			InspectBus(m_eventView.selectedIndex);
		}

		private void OnBusConstructed() {
			m_eventView.RefreshItems();
		}

		private void OnBusUpdated(Type busType) {
			int busIndex = EventBus.Editor_Buses.IndexOf(busType);
			Debug.Assert(busIndex != -1);

			m_eventView.RefreshItem(busIndex);

			if (m_eventView.selectedIndex == busIndex)
				InspectBus(busIndex);
		}

		private void InspectBus(int index) {
			(IList listeners, IList callbacks) = GetBusSubscribers(index);

			m_listenersView.itemsSource = listeners;
			m_callbacksView.itemsSource = callbacks;

			m_listenersView.RefreshItems();
			m_callbacksView.RefreshItems();
		}

		#region Listener CRUD

		private VisualElement MakeListenerItem() {
			Label label = new Label();
			label.AddToClassList("SubscriberItem");

			return label;
		}

		private void BindListenerItem(VisualElement visualElement, int i) {
			object listener = m_listenersView.itemsSource[i];
			string listenerName = listener is UnityEngine.Object obj && obj != null ? $"{obj.name} ({obj.GetType().Name})" : $"({listener.GetType().Name})";
			visualElement.Q<Label>().text = listenerName;

			if (m_listenerClickables.Count <= i)
				m_listenerClickables.Add(new Clickable(() => PingListener(i)));

			visualElement.AddManipulator(m_listenerClickables[i]);
		}

		private void UnbindListenerItem(VisualElement visualElement, int i) {
			visualElement.RemoveManipulator(m_listenerClickables[i]);
		}


		#endregion

		private void PingListener(int index) {
			if (m_listenersView.itemsSource[index] is UnityEngine.Object obj)
				EditorGUIUtility.PingObject(obj);
		}

		#region Callback CRUD

		private VisualElement MakeCallbackItem() {
			Label label = new Label();
			label.AddToClassList("SubscriberItem");

			return label;
		}

		private void BindCallbackItem(VisualElement visualElement, int i) {
			Delegate callback = (Delegate)m_callbacksView.itemsSource[i];
			visualElement.Q<Label>().text = $"Target: {callback.Target} | Method: {callback.Method.Name}";

			if (m_callbackClickables.Count <= i)
				m_callbackClickables.Add(new Clickable(() => PingCallback(i)));

			visualElement.AddManipulator(m_callbackClickables[i]);
		}

		private void UnbindCallbackItem(VisualElement visualElement, int i) {
			visualElement.RemoveManipulator(m_callbackClickables[i]);
		}

		#endregion

		private void PingCallback(int index) {
			if (((Delegate)m_callbacksView.itemsSource[index]).Target is UnityEngine.Object obj)
				EditorGUIUtility.PingObject(obj);
		}

		private (IList listeners, IList callbacks) GetBusSubscribers(int index) {
			Type busType = EventBus.Editor_Buses[index];

			return (
				(IList)busType.GetField("s_listeners", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null),
				(IList)busType.GetField("s_callbacks", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)
			);
		}
	}
}
