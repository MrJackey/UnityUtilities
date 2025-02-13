using System.Collections.Generic;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class BehaviourGraph : VisualElement, ISelectionManager {
		protected SerializedObject m_serializedBehaviour;

		protected ActionInspector m_actionInspector = new();
		protected BlackboardInspector m_blackboardInspector = new();
		protected ConnectionManipulator m_connectionManipulator;

		protected Label m_graphHeader;
		protected Label m_graphInstanceInfo;

		protected List<Node> m_nodes = new();
		protected List<Connection> m_connections = new();

		protected bool m_isEditable;
		protected Vector2 m_createNodePosition;

		public override VisualElement contentContainer { get; }
		public SerializedObject SerializedBehaviour => m_serializedBehaviour;
		public virtual ObjectBehaviour Behaviour => null;

		public BlackboardInspector BlackboardInspector => m_blackboardInspector;

		VisualElement ISelectionManager.Element => this;
		public List<ISelectableElement> SelectedElements { get; } = new();

		public bool IsEditable => m_isEditable;

		public BehaviourGraph() {
			usageHints = UsageHints.GroupTransform;

			hierarchy.Add(new GraphBackground());
			hierarchy.Add(contentContainer = new VisualElement() {
				name = "GraphContent",
				usageHints = UsageHints.GroupTransform,
			});
			hierarchy.Add(m_graphHeader = new Label() {
				name = "BehaviourName",
				bindingPath = "m_Name",
				pickingMode = PickingMode.Ignore,
			});
			hierarchy.Add(m_graphInstanceInfo = new Label() {
				name = "InstanceInfo",
				pickingMode = PickingMode.Ignore,
			});

			hierarchy.Add(m_actionInspector);
			hierarchy.Add(m_blackboardInspector);

			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new RectangleSelector(this));

			this.AddManipulator(m_connectionManipulator = new ConnectionManipulator(this));
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionValidator = IsConnectionValid;

			AddToClassList(nameof(BehaviourGraph));
		}

		public void Tick() {
			foreach (VisualElement child in contentContainer.Children()) {
				if (child is ITickElement tickElement) {
					tickElement.Tick();
				}
			}
		}

		protected void ClearGraph() {
			Clear();
			m_nodes.Clear();
			m_connections.Clear();
		}

		public virtual void BeginNodeCreation() {
			SaveCreatePosition();
		}

		protected void SaveCreatePosition() {
			m_createNodePosition = Event.current.mousePosition;
		}

		public void AddNode(Node node) {
			// This needs to listen before any draggers. Otherwise, it won't receive its
			// events as they stop propagation to prevent conflicts with each other
			Clickable doubleClickManipulator = new Clickable(() => OnNodeDoubleClick(node));
			doubleClickManipulator.activators.Clear();
			doubleClickManipulator.activators.Add(new ManipulatorActivationFilter() {
				button = MouseButton.LeftMouse,
				clickCount = 2,
			});
			node.AddManipulator(doubleClickManipulator);

			if (m_isEditable) {
				node.AddManipulator(new SelectionDragger(this));
				node.AddManipulator(new Dragger());
			}

			node.AddManipulator(new ClickSelector(this));

			Add(node);
			m_nodes.Add(node);

			OnNodeAdded(node);
		}
		protected virtual void OnNodeAdded(Node node) { }
		protected virtual void OnNodeDoubleClick(Node node) { }

		public void RemoveNode(Node node) {
			OnNodeRemoval(node);

			node.RemoveFromHierarchy();
			bool removed = m_nodes.Remove(node);
			Debug.Assert(removed);
		}
		protected virtual void OnNodeRemoval(Node node) { }

		protected virtual bool IsConnectionValid(IConnectionSocket start, IConnectionSocket end) => true;

		private void OnConnectionCreated(Connection connection) {
			m_connections.Add(connection);
			connection.SendToBack();

			connection.RegisterCallback<MouseDownEvent>(OnConnectionMouseDown);
		}

		public void AddConnection(Connection connection) {
			Insert(0, connection);
			m_connections.Add(connection);

			connection.RegisterCallback<MouseDownEvent>(OnConnectionMouseDown);
		}

		public void RemoveConnection(Connection connection) {
			if (connection.Start != null)
				connection.Start.OutgoingConnections--;

			if (connection.End != null)
				connection.End.IncomingConnections--;

			connection.RemoveFromHierarchy();
			bool removed = m_connections.Remove(connection);
			Debug.Assert(removed);

			connection.UnregisterCallback<MouseDownEvent>(OnConnectionMouseDown);
		}

		private void OnConnectionMouseDown(MouseDownEvent evt) {
			if (evt.button != 0) return;
			if (!m_isEditable) return;

			foreach (Connection connection in m_connections) {
				if (!connection.CheckClick(evt))
					continue;

				evt.StopImmediatePropagation();

				IConnectionSocket closestSocket = connection.GetClosestSocket(evt);
				m_connectionManipulator.MoveConnection(connection, closestSocket);
				break;
			}
		}

		public void OnSelectionChange() {
			if (SelectedElements.Count == 1)
				InspectElement(SelectedElements[0].Element);
			else
				ClearInspection();
		}

		protected virtual void InspectElement(VisualElement element) { }
		public void ClearInspection() => m_actionInspector.ClearInspection();

		public virtual void Duplicate() { }
	}

	public class BehaviourGraph<T> : BehaviourGraph where T : ObjectBehaviour {
		protected T m_behaviour;

		public override ObjectBehaviour Behaviour => m_behaviour;
	}
}
