using System.Collections.Generic;
using Jackey.Behaviours.Editor.Manipulators;
using Jackey.Behaviours.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class BehaviourGraph : VisualElement, ISelectionManager {
		protected SerializedObject m_serializedBehaviour;

		protected Inspector m_inspector;
		protected BlackboardInspector m_blackboardInspector;
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

		public List<Node> Nodes => m_nodes;
		public List<Connection> Connections => m_connections;
		public List<GraphGroup> Groups => m_groups;

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

			hierarchy.Add(m_inspector = new Inspector());
			m_inspector.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
			hierarchy.Add(m_blackboardInspector = new BlackboardInspector());
			m_blackboardInspector.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());

			GraphMinimap minimap = new GraphMinimap(this);
			hierarchy.Add(minimap);
			minimap.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());

			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new RectangleSelector(this));

			this.AddManipulator(m_connectionManipulator = new ConnectionManipulator(InsertConnectionElement));
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionValidator = IsConnectionValid;

			m_groupCreator = new GraphGroupCreator();
			m_groupCreator.GroupCreated += OnGroupCreated;

			AddToClassList(nameof(BehaviourGraph));
		}

		public virtual void UpdateBehaviour(ObjectBehaviour behaviour) { }

		public virtual void Tick() {
			foreach (VisualElement child in contentContainer.Children()) {
				if (child is ITickElement tickElement) {
					tickElement.Tick();
				}
			}

			EditorUtility.SetDirty(m_serializedBehaviour.targetObject);
		}
		protected virtual void UpdateEditorData() { }

		protected void BuildGraph() {
			if (m_isEditable)
				this.AddManipulator(m_groupCreator);

			SyncGraph();
		}

		protected virtual void SyncGraph() { }

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

		/// <summary>
		/// Add a visual node to the graph
		/// </summary>
		public void AddNode(Node node) {
			Debug.Assert(!m_nodes.Contains(node));

			// This needs to listen before any draggers. Otherwise, it won't receive its
			// events as they stop propagation to prevent conflicts with each other
			Clickable doubleClickManipulator = new Clickable(() => OnNodeDoubleClick(node));
			doubleClickManipulator.activators.Clear();
			doubleClickManipulator.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, clickCount = 2 });
			node.AddManipulator(doubleClickManipulator);

			if (m_isEditable) {
				SelectionDragger selectionDragger = new SelectionDragger(this);
				selectionDragger.Moved += OnElementMoved;
				node.AddManipulator(selectionDragger);

				Dragger dragger = new Dragger();
				dragger.Moved += OnElementMoved;
				node.AddManipulator(dragger);
			}

			node.AddManipulator(new ClickSelector(this));

			Add(node);
			m_nodes.Add(node);

			OnNodeAdded(node);
		}

		/// <summary>
		/// Callback for when a visual node is added to the graph
		/// </summary>
		protected virtual void OnNodeAdded(Node node) { }
		protected virtual void OnNodeDoubleClick(Node node) { }

		public virtual void DeleteNode(Node node) { }

		/// <summary>
		/// Remove a node's visuals from the graph
		/// </summary>
		public void RemoveNode(Node node) {
			OnNodeRemoval(node);

			node.RemoveFromHierarchy();
			bool removed = m_nodes.Remove(node);
			Debug.Assert(removed);
		}

		/// <summary>
		/// Callback for when node's visuals are removed from the graph
		/// </summary>
		protected virtual void OnNodeRemoval(Node node) { }

		// Moves nodes to position keeping relative offsets
		protected void MoveNodesAroundPoint(in Node[] nodes, Vector2 position) {
			Rect nodeRect = new Rect(nodes[0].transform.position, Vector2.zero);

			for (int i = 1; i < nodes.Length; i++) {
				Vector3 nodePosition = nodes[i].transform.position;
				nodeRect = nodeRect.Encapsulate(nodePosition);
			}

			Vector2 offset = position - nodeRect.center;
			foreach (Node node in nodes)
				node.transform.position += (Vector3)offset;
		}

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
			InsertConnectionElement(connection);

			connection.RegisterCallback<MouseDownEvent>(OnConnectionMouseDown);
		}

		private void InsertConnectionElement(Connection connection) {
			Insert(m_groups.Count, connection);
		}

		public void RemoveAllConnections() {
			for (int i = m_connections.Count - 1; i >= 0; i--)
				RemoveConnection(m_connections[i]);
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

			evt.StopImmediatePropagation();

			Connection connection = (Connection)evt.currentTarget;
			IConnectionSocket closestSocket = connection.GetClosestSocket(evt);
			m_connectionManipulator.MoveConnection(connection, closestSocket);
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
			OnGroupCreate(group);
			AddGroup(group);

			ApplyChanges();
		}
		protected virtual void OnGroupCreate(GraphGroup group) { }

		/// <summary>
		/// Add a visual group to the graph
		/// </summary>
		public void AddGroup(GraphGroup group) {
			Debug.Assert(!m_groups.Contains(group));

			group.Dragger.Moved += OnElementMoved;
			group.GroupDragger.Moved += OnElementMoved;
			group.Resizer.Resized += OnElementResized;

			group.Modified += OnGroupModified;

			group.AddManipulator(new ClickSelector(this));
			group.SelectionManager = this;

			m_groups.Add(group);
			Insert(0, group);

			OnGroupAdded(group);
		}

		/// <summary>
		/// Callback for when a visual group has been added to the graph
		/// </summary>
		protected virtual void OnGroupAdded(GraphGroup group) { }

		public virtual void DeleteGroup(GraphGroup group) { }

		/// <summary>
		/// Remove a visual group from the graph
		/// </summary>
		public void RemoveGroup(GraphGroup group) {
			group.RemoveFromHierarchy();
			bool removed = m_groups.Remove(group);
			Debug.Assert(removed);
		}

		#endregion

		#region Actions

		public void DeleteSelection() {
			if (SelectedElements.Count == 0)
				return;

			ClearInspection();

			int undoGroup = UndoUtilities.CreateGroup("Delete selected elements");

			foreach (ISelectableElement selectedElement in SelectedElements) {
				if (selectedElement.Element is Node node)
					DeleteNode(node);
				else if (selectedElement.Element is GraphGroup group)
					DeleteGroup(group);
			}

			Undo.CollapseUndoOperations(undoGroup);
			ApplyChanges();

			SelectedElements.Clear();
			OnSelectionChange();
		}

		public void SmartDeleteSelection() {
			if (SelectedElements.Count == 0)
				return;

			int undoGroup = UndoUtilities.CreateGroup("Smart delete selected elements");

			ClearInspection();

			foreach (ISelectableElement selectedElement in SelectedElements)
				SmartDelete(selectedElement.Element);

			Undo.CollapseUndoOperations(undoGroup);

			ApplyChanges();

			SelectedElements.Clear();
			OnSelectionChange();
		}

		protected void SmartDelete(VisualElement element) {
			Undo.RecordObject(m_serializedBehaviour.targetObject, "Smart delete element");

			OnSmartDeletion(element);

			if (element is Node node)
				DeleteNode(node);
			else if (element is GraphGroup group)
				DeleteGroup(group);

			ApplyChanges();
		}
		protected virtual void OnSmartDeletion(VisualElement element) { }

		public virtual void DuplicateSelection() { }
		public virtual void CopySelection() { }
		public virtual void Paste(Vector2 GUIPosition) { }

		public void UndoRedo() {
			m_serializedBehaviour.Update();

			m_connectionManipulator.Cancel();

			int oldNodeCount = m_nodes.Count;
			int oldGroupCount = m_groups.Count;

			SyncGraph();

			// Inspectable elements may have their indices changed thus disconnecting any active inspector
			if (m_nodes.Count != oldNodeCount || m_groups.Count != oldGroupCount)
				OnSelectionChange();
		}

		#endregion

		private void OnElementMoved(VisualElement _, Vector2 from, Vector2 to) {
			// Ignore creating undo for minor moves. They can quickly become annoying
			if (Vector2.SqrMagnitude(to - from) > 10 * 10)
				Undo.RecordObject(m_serializedBehaviour.targetObject, "Move element(s)");

			ApplyChanges();
		}

		private void OnElementResized() {
			Undo.RecordObject(m_serializedBehaviour.targetObject, "Resize element");
			ApplyChanges();
		}

		private void OnGroupModified() {
			Undo.RecordObject(m_serializedBehaviour.targetObject, "Modify group");
			ApplyChanges();
		}

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

		protected virtual void ApplyChanges() {
			UpdateEditorData();
			m_serializedBehaviour.ApplyModifiedPropertiesWithoutUndo();
			m_serializedBehaviour.Update();
		}
	}

	public class BehaviourGraph<T> : BehaviourGraph where T : ObjectBehaviour {
		protected T m_behaviour;

		public override ObjectBehaviour Behaviour => m_behaviour;

		public override void UpdateBehaviour(ObjectBehaviour behaviour) {
			if (behaviour is not T typedBehaviour) return;

			m_serializedBehaviour?.Dispose();

			m_behaviour = typedBehaviour;
			m_serializedBehaviour = new SerializedObject(behaviour);

			bool isPersistent = EditorUtility.IsPersistent(m_behaviour);
			m_graphInstanceInfo.text = isPersistent ? "(Asset)" : "(Instance)";
			m_isEditable = isPersistent;

			ClearGraph();
			BuildGraph();

			m_graphHeader.Bind(m_serializedBehaviour);

			m_blackboardInspector.SetSecondaryBlackboard(behaviour.Blackboard, m_serializedBehaviour.FindProperty(nameof(ObjectBehaviour.m_blackboard)));

			this.ClearSelection();
			OnSelectionChange();
		}

		protected override void SyncGraph() {
			base.SyncGraph();

			// Remove excess groups
			for (int i = m_groups.Count - 1; i >= m_behaviour.Editor_Data.Groups.Count; i--) {
				RemoveGroup(m_groups[i]);
			}

			// Sync/add missing groups
			for (int i = 0; i < m_behaviour.Editor_Data.Groups.Count; i++) {
				ObjectBehaviour.EditorData.Group dataGroup = m_behaviour.Editor_Data.Groups[i];

				if (i >= m_groups.Count)
					AddGroup(new GraphGroup(dataGroup.Rect));
				else
					m_groups[i].Reposition(dataGroup.Rect);

				m_groups[i].Label = dataGroup.Label;
				m_groups[i].SetAutoSize(dataGroup.AutoSize);
			}
		}

		protected override void UpdateEditorData() {
			for (int i = 0; i < m_groups.Count; i++) {
				GraphGroup graphGroup = m_groups[i];
				ObjectBehaviour.EditorData.Group dataGroup = m_behaviour.Editor_Data.Groups[i];

				dataGroup.Label = graphGroup.Label;
				dataGroup.AutoSize = graphGroup.AutoSize;
				dataGroup.Rect = new Rect(graphGroup.transform.position, new Vector2(graphGroup.style.width.value.value, graphGroup.style.height.value.value));
			}
		}

		protected override void OnGroupCreate(GraphGroup group) {
			Undo.RecordObject(m_behaviour, "Create group");

			m_behaviour.Editor_Data.Groups.Add(new ObjectBehaviour.EditorData.Group());
		}

		protected override void OnGroupAdded(GraphGroup group) {
			int groupIndex = m_groups.IndexOf(group);

			Debug.Assert(groupIndex != -1);
			ObjectBehaviour.EditorData.Group dataGroup = m_behaviour.Editor_Data.Groups[groupIndex];

			group.Label = dataGroup.Label;
			group.SetAutoSize(dataGroup.AutoSize);
		}

		public override void DeleteGroup(GraphGroup group) {
			Undo.RecordObject(m_behaviour, "Delete group");

			int groupIndex = m_groups.IndexOf(group);

			Debug.Assert(groupIndex != -1);
			m_behaviour.Editor_Data.Groups.RemoveAt(groupIndex);

			RemoveGroup(group);
			ApplyChanges();
		}
	}
}
