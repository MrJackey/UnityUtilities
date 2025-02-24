using System.Collections.Generic;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class BehaviourGraph : VisualElement, ISelectionManager {
		protected SerializedObject m_serializedBehaviour;

		protected Inspector m_inspector = new();
		protected BlackboardInspector m_blackboardInspector = new();
		protected ConnectionManipulator m_connectionManipulator;
		protected GraphGroupCreator m_groupCreator;

		protected Label m_graphHeader;
		protected Label m_graphInstanceInfo;

		protected List<Node> m_nodes = new();
		protected List<Connection> m_connections = new();
		protected List<GraphGroup> m_groups = new();

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

			hierarchy.Add(m_inspector);
			m_inspector.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
			hierarchy.Add(m_blackboardInspector);
			m_blackboardInspector.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());

			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new RectangleSelector(this));

			this.AddManipulator(m_connectionManipulator = new ConnectionManipulator(this));
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionValidator = IsConnectionValid;

			m_groupCreator = new GraphGroupCreator();
			m_groupCreator.GroupCreated += OnGroupCreated;

			AddToClassList(nameof(BehaviourGraph));
		}

		public void Tick() {
			foreach (VisualElement child in contentContainer.Children()) {
				if (child is ITickElement tickElement) {
					tickElement.Tick();
				}
			}

			UpdateEditorData();

			EditorUtility.SetDirty(m_serializedBehaviour.targetObject);
		}
		protected virtual void UpdateEditorData() { }

		protected virtual void BuildGraph() {
			if (m_isEditable) {
				this.AddManipulator(m_groupCreator);
			}
		}

		protected void ClearGraph() {
			Clear();
			m_nodes.Clear();
			m_connections.Clear();
			m_groups.Clear();

			this.RemoveManipulator(m_groupCreator);
		}

		public virtual void BeginNodeCreation(Vector2 GUIPosition) {
			m_createNodePosition = GUIPosition;
		}

		protected void SaveCreatePosition() {
			m_createNodePosition = Event.current.mousePosition;
		}

		#region Node

		public void AddNode(Node node) {
			Debug.Assert(!m_nodes.Contains(node));

			// This needs to listen before any draggers. Otherwise, it won't receive its
			// events as they stop propagation to prevent conflicts with each other
			Clickable doubleClickManipulator = new Clickable(() => OnNodeDoubleClick(node));
			doubleClickManipulator.activators.Clear();
			doubleClickManipulator.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, clickCount = 2 });
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

		#endregion

		#region Connection

		protected virtual bool IsConnectionValid(IConnectionSocket start, IConnectionSocket end) => true;

		private void OnConnectionCreated(Connection connection) {
			m_connections.Add(connection);
			Insert(m_groups.Count, connection);

			connection.RegisterCallback<MouseDownEvent>(OnConnectionMouseDown);
		}

		public void AddConnection(Connection connection) {
			Debug.Assert(!m_connections.Contains(connection));

			m_connections.Add(connection);
			Insert(m_groups.Count, connection);

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

		protected void MoveConnection(Connection connection, IConnectionSocket from, IConnectionSocket to) {
			if (from == connection.Start) {
				from.OutgoingConnections--;
				connection.Start = to;
				to.OutgoingConnections++;
			}
			else {
				from.IncomingConnections--;
				connection.End = to;
				to.IncomingConnections++;
			}
		}

		#endregion

		#region Group

		private void OnGroupCreated(GraphGroup group) {
			m_groups.Add(group);
			group.SendToBack();

			group.AddManipulator(new ClickSelector(this));
			group.SelectionManager = this;

			OnGroupCreate(group);
		}
		protected virtual void OnGroupCreate(GraphGroup group) { }

		public void AddGroup(GraphGroup group) {
			Debug.Assert(!m_groups.Contains(group));

			group.AddManipulator(new ClickSelector(this));
			group.SelectionManager = this;

			m_groups.Add(group);
			Insert(0, group);

			OnGroupAdded(group);
		}
		protected virtual void OnGroupAdded(GraphGroup group) { }

		public void RemoveGroup(GraphGroup group) {
			OnGroupRemoval(group);

			group.RemoveFromHierarchy();
			bool removed = m_groups.Remove(group);
			Debug.Assert(removed);
		}
		protected virtual void OnGroupRemoval(GraphGroup group) { }

		#endregion

		#region Actions

		public void DeleteSelection() {
			if (SelectedElements.Count == 0)
				return;

			ClearInspection();

			foreach (ISelectableElement selectedElement in SelectedElements) {
				if (selectedElement.Element is Node node)
					RemoveNode(node);
				else if (selectedElement.Element is GraphGroup group)
					RemoveGroup(group);
			}

			// TODO: Add undo
			SerializedBehaviour.Update();

			SelectedElements.Clear();
			OnSelectionChange();
		}

		public void SoftDeleteSelection() {
			if (SelectedElements.Count == 0)
				return;

			ClearInspection();

			foreach (ISelectableElement selectedElement in SelectedElements)
				SoftDelete(selectedElement.Element);

			// TODO: Add undo
			SerializedBehaviour.Update();

			SelectedElements.Clear();
			OnSelectionChange();
		}

		protected void SoftDelete(VisualElement element) {
				OnSoftDeletion(element);

				if (element is Node node)
					RemoveNode(node);
				else if (element is GraphGroup group)
					RemoveGroup(group);

				SerializedBehaviour.Update();
		}
		protected virtual void OnSoftDeletion(VisualElement element) { }

		public virtual void DuplicateSelection() { }

		#endregion

		public void OnSelectionChange() {
			if (SelectedElements.Count != 1) {
				ClearInspection();
				return;
			}

			if (SelectedElements[0].Element is GraphGroup group)
				InspectGroup(group);
			else
				InspectElement(SelectedElements[0].Element);
		}

		private void InspectGroup(GraphGroup group) {
			int groupIndex = m_groups.IndexOf(group);
			Debug.Assert(groupIndex != -1);

			SerializedProperty groupProperty = m_serializedBehaviour.FindProperty($"Editor_Data.Groups.Array.data[{groupIndex}]");
			m_inspector.Inspect(typeof(ObjectBehaviour.EditorData.Group), groupProperty);
		}
		protected virtual void InspectElement(VisualElement element) { }
		public void ClearInspection() => m_inspector.ClearInspection();
	}

	public class BehaviourGraph<T> : BehaviourGraph where T : ObjectBehaviour {
		protected T m_behaviour;

		public override ObjectBehaviour Behaviour => m_behaviour;

		protected override void UpdateEditorData() {
			for (int i = 0; i < m_groups.Count; i++) {
				GraphGroup graphGroup = m_groups[i];
				ObjectBehaviour.EditorData.Group dataGroup = m_behaviour.Editor_Data.Groups[i];

				dataGroup.Label = graphGroup.Label;
				dataGroup.AutoSize = graphGroup.AutoSize;
				dataGroup.Rect = new Rect(graphGroup.transform.position, graphGroup.layout.size);
			}
		}

		protected override void OnGroupCreate(GraphGroup group) {
			m_behaviour.Editor_Data.Groups.Add(new ObjectBehaviour.EditorData.Group());
		}

		protected override void OnGroupAdded(GraphGroup group) {
			int groupIndex = m_groups.IndexOf(group);

			Debug.Assert(groupIndex != -1);
			ObjectBehaviour.EditorData.Group dataGroup = m_behaviour.Editor_Data.Groups[groupIndex];

			group.Label = dataGroup.Label;
			group.SetAutoSize(dataGroup.AutoSize);
		}

		protected override void OnGroupRemoval(GraphGroup group) {
			int groupIndex = m_groups.IndexOf(group);

			Debug.Assert(groupIndex != -1);
			m_behaviour.Editor_Data.Groups.RemoveAt(groupIndex);
		}
	}
}
