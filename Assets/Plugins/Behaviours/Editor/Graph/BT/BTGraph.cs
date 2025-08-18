using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.BT.Nested;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.CopyPaste;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Editor.Utilities;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.BT {
	public class BTGraph : BehaviourGraph<BehaviourTree> {
		internal static readonly Type[] s_actionTypes = TypeCache.GetTypesDerivedFrom<BehaviourAction>().Where(type => !type.IsAbstract).ToArray();

		public BTGraph() {
			m_connectionManipulator.ConnectionVoided += OnConnectionVoided;
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionMoved += OnConnectionMoved;
			m_connectionManipulator.ConnectionRemoved += OnConnectionRemoved;
		}

		protected override void SyncGraph() {
			base.SyncGraph();

			// Remove excess nodes
			for (int i = m_nodes.Count - 1; i >= 0; i--) {
				BTNode node = (BTNode)m_nodes[i];
				if (m_behaviour.m_allActions.Contains(node.Action))
					continue;

				RemoveNode(node);
			}

			// Add missing nodes
			foreach (BehaviourAction action in m_behaviour.m_allActions) {
				if (GetNodeOfAction(action) != null)
					continue;

				AddNode(new BTNode(action));
			}

			// Sync connections by removing and then recreating them
			RemoveAllConnections();

			foreach (BehaviourAction action in m_behaviour.m_allActions) {
				BTNode node = GetNodeOfAction(action);
				Debug.Assert(node != null);

				node.transform.position = action.Editor_Data.Position;
				node.SetEntry(m_behaviour.m_entry == action);

				switch (action) {
					case Composite composite:
						foreach (BehaviourAction child in composite.Children) {
							AddConnection(new Connection(
								start: node.OutSocket,
								end: GetNodeOfAction(child)
							));
						}

						break;
					case Decorator { Child: not null } decorator:
						AddConnection(new Connection(
							start: node.OutSocket,
							end: GetNodeOfAction(decorator.Child)
						));
						break;
				}
			}

			this.ValidateSelection();
		}

		protected override void UpdateEditorData() {
			base.UpdateEditorData();

			foreach (BTNode node in m_nodes) {
				node.UpdateEditorData();
			}

			// Order after updating ALL data to ensure up-to-date information
			foreach (BTNode node in m_nodes) {
				if (node.Action is Composite composite)
					composite.Editor_OrderChildren();
			}
		}

		#region Node CRUD

		public override void BeginNodeCreation(Vector2 GUIPosition) {
			base.BeginNodeCreation(GUIPosition);

			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(GUIPosition);
			TypeProvider.Instance.AskForType(mouseScreenPosition, s_actionTypes, type => CreateNode(type));
		}

		private BTNode CreateNode(Type type) {
			int undoGroup = UndoUtilities.CreateGroup($"Create {type.Name} node");
			Undo.RecordObject(m_behaviour, $"Create {type.Name} node");

			BehaviourAction action = (BehaviourAction)Activator.CreateInstance(type);
			BTNode node = new BTNode(action);
			m_behaviour.m_allActions.Add(action);

			Vector2 createPosition = this.ChangeCoordinatesTo(contentContainer, m_createNodePosition);
			createPosition.x -= Node.DEFAULT_WIDTH / 2f;
			createPosition.y -= Node.DEFAULT_HEIGHT / 2f;
			node.transform.position = createPosition;

			AddNode(node);

			ApplyChanges();
			Undo.CollapseUndoOperations(undoGroup);

			this.ReplaceSelection(node);

			return node;
		}

		protected override void OnNodeAdded(Node node) {
			BTNode btNode = (BTNode)node;
			btNode.SetEntry(m_behaviour.m_entry == btNode.Action);

			btNode.AddManipulator(new ContextualMenuManipulator(ShowNodeContext));

			if (m_isEditable) {
				ConnectionSocket outSocket = btNode.OutSocket;
				outSocket.RegisterCallback<MouseDownEvent, IConnectionSocket>(OnSocketMouseDown, outSocket);
			}
		}

		public override void DeleteNode(Node node) {
			BTNode btNode = (BTNode)node;
			BehaviourAction action = btNode.Action;

			Undo.RecordObject(m_behaviour, $"Delete {action.GetType().Name} node");

			if (action == m_behaviour.m_entry)
				m_behaviour.m_entry = null;

			foreach (BehaviourAction behaviourAction in m_behaviour.m_allActions) {
				switch (behaviourAction) {
					case Composite composite:
						if (composite.Children.Remove(action))
							goto breakLoop;

						break;
					case Decorator decorator:
						if (decorator.Child == action) {
							decorator.Child = null;
							goto breakLoop;
						}

						break;
				}
			}
			breakLoop:;

			bool removed = m_behaviour.m_allActions.Remove(action);
			Debug.Assert(removed);

			ApplyChanges();

			RemoveNode(node);
		}

		protected override void OnNodeRemoval(Node node) {
			BTNode btNode = (BTNode)node;

			for (int i = m_connections.Count - 1; i >= 0; i--) {
				Connection connection = m_connections[i];

				if (connection.Start.Element == btNode.OutSocket || connection.End.Element == node) {
					RemoveConnection(connection);
				}
			}
		}

		public override void DuplicateSelection() {
			if (SelectedElements.Count == 0) return;

			List<BTNode> originals = new List<BTNode>();
			List<BTNode> clones = new List<BTNode>();

			Undo.RecordObject(m_behaviour, "Duplicate selected elements");

			// Duplicate Nodes
			foreach (ISelectableElement selectedElement in SelectedElements) {
				if (selectedElement is not BTNode btNode)
					continue;

				BehaviourAction actionClone = SerializationUtilities.DeepClone(btNode.Action);
				BTNode nodeClone = new BTNode(actionClone);

				originals.Add(btNode);
				clones.Add(nodeClone);

				m_behaviour.m_allActions.Add(actionClone);

				AddNode(nodeClone);
				nodeClone.transform.position += Node.DUPLICATE_OFFSET;
			}

			ApplyChanges();

			// Clear/Duplicate Connections
			for (int i = 0; i < clones.Count; i++) {
				BTNode original = originals[i];
				BTNode clone = clones[i];

				int childCloneIndex;

				switch (clone.Action) {
					case Composite composite:
						composite.Children.Clear();

						Debug.Assert(original.Action is Composite);
						Composite originalComposite = (Composite)original.Action;

						foreach (BehaviourAction originalChild in originalComposite.Children) {
							childCloneIndex = originals.FindIndex(x => x.Action == originalChild);
							if (childCloneIndex == -1) continue;

							composite.Children.Add(clones[childCloneIndex].Action);

							AddConnection(new Connection(
								start: clone.OutSocket,
								end: clones[childCloneIndex]
							));
						}
						break;

					case Decorator decorator:
						decorator.Child = null;

						Debug.Assert(original.Action is Decorator);
						Decorator originalDecorator = (Decorator)original.Action;

						childCloneIndex = originals.FindIndex(x => x.Action == originalDecorator.Child);
						if (childCloneIndex == -1) break;

						decorator.Child = clones[childCloneIndex].Action;

						AddConnection(new Connection(
							start: clone.OutSocket,
							end: clones[childCloneIndex]
						));
						break;
				}
			}

			this.ReplaceSelection(clones);
		}

		public override void CopySelection() {
			if (SelectedElements.Count == 0) return;

			// Get all selected actions
			List<BehaviourAction> actions = new List<BehaviourAction>();
			foreach (ISelectableElement element in SelectedElements) {
				if (element.Element is BTNode btNode)
					actions.Add(btNode.Action);
			}

			if (actions.Count == 0)
				return;

			// Save their parents if part of selection
			int[] parentIndices = new int[actions.Count];
			for (int i = 0; i < actions.Count; i++) {
				parentIndices[i] = -1;

				BehaviourAction action = actions[i];
				Connection connection = GetConnectionToNode(GetNodeOfAction(action));
				if (connection == null) continue;

				BehaviourAction parent = connection.Start.Element.GetFirstAncestorOfType<BTNode>().Action;
				int parentIndex = actions.IndexOf(parent);
				if (parentIndex == -1) continue;

				parentIndices[i] = parentIndex;
			}

			// Save data to clipboard
			BTCopyData btData = new BTCopyData() {
				ActionTypes = actions.Select(action => action.GetType().AssemblyQualifiedName).ToArray(),
				Actions = actions.Select(JsonUtility.ToJson).ToArray(),
				ParentIndices = parentIndices,
			};
			CopyPasteData copyData = new CopyPasteData() {
				Context = CopyPasteContext.BT,
				Data = JsonUtility.ToJson(btData),
			};
			GUIUtility.systemCopyBuffer = JsonUtility.ToJson(copyData);
		}

		public override void Paste(Vector2 GUIPosition) {
			CopyPasteData pasteData;
			try {
				pasteData = JsonUtility.FromJson<CopyPasteData>(GUIUtility.systemCopyBuffer);
			}
			catch (ArgumentException) {
				Debug.LogWarning("Invalid clipboard content. Unable to paste");
				return;
			}

			if (pasteData is not { Context: CopyPasteContext.BT }) return;

			BTCopyData btData;
			try {
				btData = JsonUtility.FromJson<BTCopyData>(pasteData.Data);
			}
			catch (ArgumentException) {
				Debug.LogWarning("Invalid clipboard content. Unable to paste");
				return;
			}

			Undo.RecordObject(m_behaviour, "Paste nodes");

			// Create nodes
			BehaviourAction[] actions = new BehaviourAction[btData.Actions.Length];
			BTNode[] nodes = new BTNode[btData.Actions.Length];
			for (int i = 0; i < actions.Length; i++) {
				BehaviourAction action = (BehaviourAction)JsonUtility.FromJson(btData.Actions[i], Type.GetType(btData.ActionTypes[i]));
				actions[i] = action;
				m_behaviour.m_allActions.Add(action);

				switch (action) {
					case Composite composite:
						composite.Children.Clear();
						break;
					case Decorator decorator:
						decorator.Child = null;
						break;
				}

				BTNode node = new BTNode(action);
				nodes[i] = node;
				AddNode(node);
			}

			// Setup connections
			for (int i = 0; i < btData.ParentIndices.Length; i++) {
				int parentIndex = btData.ParentIndices[i];
				if (parentIndex == -1) continue;

				BehaviourAction parentAction = actions[parentIndex];
				Debug.Assert(parentAction is Composite or Decorator);

				switch (parentAction) {
					case Composite composite:
						composite.Children.Add(actions[i]);
						break;
					case Decorator decorator:
						decorator.Child = actions[i];
						break;
				}

				AddConnection(new Connection(nodes[parentIndex].OutSocket, nodes[i]));
			}

			// Move nodes to cursor location keeping relative offsets
			Vector2 pasteCenter = this.ChangeCoordinatesTo(contentContainer, GUIPosition) - new Vector2(Node.DEFAULT_WIDTH / 2f, Node.DEFAULT_HEIGHT / 2f);
			Rect pasteRect = new Rect(nodes[0].transform.position, Vector2.zero);

			for (int i = 1; i < nodes.Length; i++) {
				Vector3 nodePosition = nodes[i].transform.position;
				pasteRect.min = Vector2.Min(pasteRect.min, nodePosition);
				pasteRect.max = Vector2.Max(pasteRect.max, nodePosition);
			}

			Vector2 offset = pasteCenter - pasteRect.center;
			foreach (BTNode node in nodes)
				node.transform.position += (Vector3)offset;

			ApplyChanges();
			this.ReplaceSelection(nodes);
		}

		#endregion

		protected override void OnNodeDoubleClick(Node node) {
			BTNode btNode = (BTNode)node;

			if (btNode.Action is NestedBehaviourAction nestedAction && nestedAction.InstanceOrBehaviour != null) {
				EditorWindow.GetWindow<BehaviourEditorWindow>().PushBehaviour(nestedAction.InstanceOrBehaviour);
				return;
			}

			if (btNode.Action is Composite or Decorator) {
				List<BTNode> nodeHierarchy = GetHierarchyOfNode(btNode);

				if (nodeHierarchy.Count > 1)
					this.ReplaceSelection(nodeHierarchy);
			}
		}

		private void OnSocketMouseDown(MouseDownEvent evt, IConnectionSocket socket) {
			evt.StopImmediatePropagation();

			if (socket.MaxOutgoingConnections != -1 && socket.OutgoingConnections >= socket.MaxOutgoingConnections)
				return;

			m_connectionManipulator.CreateConnection(socket);
		}

		protected override void OnSmartDeletion(VisualElement element) {
			if (element is not BTNode btNode) return;

			Connection toConnection = GetConnectionToNode(btNode);
			if (toConnection != null) {
				IList<Connection> nodeConnections = GetConnectionsFromNode(btNode);
				if (nodeConnections.Count == 0)
					return;

				RemoveConnection(toConnection);

				BTNode parentNode = toConnection.Start.Element.GetFirstOfType<BTNode>();
				Debug.Assert(parentNode != null);

				BehaviourAction parentAction = parentNode.Action;

				foreach (Connection childConnection in nodeConnections) {
					if (parentAction.Editor_MaxChildCount > -1 && parentNode.OutSocket.OutgoingConnections >= parentAction.Editor_MaxChildCount)
						return;

					BehaviourAction connectedAction = ((BTNode)childConnection.End.Element).Action;

					Debug.Assert(parentAction is Composite or Decorator);
					if (parentAction is Composite composite)
						composite.Children.Add(connectedAction);
					else if (parentAction is Decorator decorator)
						decorator.Child = connectedAction;

					MoveConnection(childConnection, btNode.OutSocket, parentNode.OutSocket);
				}
			}
		}

		protected override void InspectElement(VisualElement element) {
			if (element is not BTNode node)
				return;

			int nodeIndex = m_behaviour.m_allActions.IndexOf(node.Action);

			Debug.Assert(nodeIndex != -1);

			SerializedProperty nodeProperty = m_serializedBehaviour.FindProperty($"{nameof(m_behaviour.m_allActions)}.Array.data[{nodeIndex}]");
			m_inspector.Inspect(node.Action.GetType(), nodeProperty);
		}

		#region Connection Callbacks

		private void OnConnectionVoided(Connection connection, Action<Connection, IConnectionSocket> restore) {
			SaveCreatePosition();

			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			TypeProvider.Instance.AskForType(mouseScreenPosition, s_actionTypes, type => {
				int undoGroup = UndoUtilities.CreateGroup($"Create {type.Name} node");

				BTNode toNode = CreateNode(type);
				restore.Invoke(connection, toNode);

				Undo.CollapseUndoOperations(undoGroup);
			});
		}

		protected override bool IsConnectionValid(IConnectionSocket start, IConnectionSocket end) {
			BTNode startNode = start.Element.GetFirstOfType<BTNode>();
			BTNode endNode = end.Element.GetFirstOfType<BTNode>();

			BehaviourAction startAction = startNode.Action;
			BehaviourAction endAction = endNode.Action;

			return Inner(endAction);

			// Disallow cyclic connections
			bool Inner(BehaviourAction action) {
				if (action == startAction)
					return false;

				switch (action) {
					case Composite composite:
						foreach (BehaviourAction childAction in composite.Children) {
							if (!Inner(childAction))
								return false;
						}

						return true;
					case Decorator decorator:
						if (decorator.Child == null)
							return true;

						return Inner(decorator.Child);
				}

				return true;
			}
		}

		private void OnConnectionCreated(Connection connection) {
			Undo.RecordObject(m_behaviour, "Create connection");

			BTNode start = connection.Start.Element.GetFirstOfType<BTNode>();
			BTNode child = connection.End.Element.GetFirstOfType<BTNode>();

			switch (start.Action) {
				case Composite composite:
					composite.Children.Add(child.Action);
					break;
				case Decorator decorator:
					decorator.Child = child.Action;
					break;
			}

			ApplyChanges();
		}

		private void OnConnectionMoved(Connection connection, IConnectionSocket from, IConnectionSocket to) {
			Undo.RecordObject(m_behaviour, "Move connection");

			BTNode fromNode = from.Element.GetFirstOfType<BTNode>();
			BTNode toNode = to.Element.GetFirstOfType<BTNode>();
			Debug.Assert(fromNode != null && toNode != null);

			bool movedStart = connection.Start == to;

			if (movedStart) {
				BTNode endNode = connection.End.Element.GetFirstOfType<BTNode>();

				// Remove from previous parent
				switch (fromNode.Action) {
					case Composite composite:
						bool result = composite.Children.Remove(endNode.Action);
						Debug.Assert(result);
						break;
					case Decorator decorator:
						Debug.Assert(decorator.Child == endNode.Action);
						decorator.Child = null;
						break;
				}

				// Add to new parent
				switch (toNode.Action) {
					case Composite composite:
						composite.Children.Add(endNode.Action);
						break;
					case Decorator decorator:
						decorator.Child = endNode.Action;
						break;
				}
			}
			else { // Moved end
				BTNode startNode = connection.Start.Element.GetFirstOfType<BTNode>();

				switch (startNode.Action) {
					case Composite composite:
						bool result = composite.Children.Remove(fromNode.Action);
						Debug.Assert(result);

						composite.Children.Add(toNode.Action);
						break;
					case Decorator decorator:
						Debug.Assert(decorator.Child == fromNode.Action);
						decorator.Child = toNode.Action;
						break;
				}
			}

			ApplyChanges();
		}

		private void OnConnectionRemoved(Connection connection, IConnectionSocket start, IConnectionSocket end) {
			Undo.RecordObject(m_behaviour, "Delete connection");

			BTNode endNode = end.Element.GetFirstOfType<BTNode>();
			Debug.Assert(endNode != null);

			foreach (BTNode node in m_nodes) {
				if (node.OutSocket == start) {
					Debug.Assert(node.Action is Composite or Decorator);

					switch (node.Action) {
						case Composite composite:
							bool result = composite.Children.Remove(endNode.Action);
							Debug.Assert(result);

							break;
						case Decorator decorator:
							Debug.Assert(decorator.Child == endNode.Action);
							decorator.Child = null;
							break;
					}

					break;
				}
			}

			m_connections.Remove(connection);
			ApplyChanges();
		}

		#endregion

		#region Context Menu

		private void ShowNodeContext(ContextualMenuPopulateEvent evt) {
			BTNode btNode = (BTNode)evt.target;
			DropdownMenuAction.Status editStatus = m_isEditable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

			evt.menu.AppendAction(
				"Entry",
				_ => { SetEntry(btNode); },
				m_behaviour.m_entry == btNode.Action ? DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled : editStatus
			);
			evt.menu.AppendAction(
				"Decorate",
				menuAction => {
					Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(menuAction.eventInfo.mousePosition);
					IEnumerable<Type> actionTypes = TypeCache.GetTypesDerivedFrom<Decorator>().Where(type => !type.IsAbstract);

					TypeProvider.Instance.AskForType(mouseScreenPosition, actionTypes, type => DecorateNode(btNode, type));
				},
				editStatus
			);
			evt.menu.AppendAction(
				"Breakpoint",
				_ => { ToggleBreakpoint(btNode); },
				btNode.Action.Editor_Data.Breakpoint ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
			);

			MonoScript script = AssetUtilities.GetScriptAsset(btNode.Action.GetType());
			if (script != null) {
				evt.menu.AppendSeparator();
				evt.menu.AppendAction(
					"Edit Script",
					_ => AssetDatabase.OpenAsset(script)
				);
			}

			evt.menu.AppendSeparator();
			evt.menu.AppendAction(
				"Smart Delete",
				_ => {
					SmartDelete(btNode);
					this.ValidateSelection();
				},
				editStatus
			);
			evt.menu.AppendAction(
				"Delete",
				_ => {
					DeleteNode(btNode);
					this.ValidateSelection();
				},
				editStatus
			);
		}

		private void SetEntry(BTNode node) {
			if (m_behaviour.m_entry == node.Action)
				return;

			Undo.RecordObject(m_behaviour, $"Set {node.Action.GetType().Name} as entry");

			if (m_behaviour.m_entry != null) {
				BTNode oldStartNode = GetNodeOfAction(m_behaviour.m_entry);
				Debug.Assert(oldStartNode != null);

				oldStartNode.SetEntry(false);
			}

			m_behaviour.m_entry = node.Action;
			node.SetEntry(true);

			ApplyChanges();
		}

		private void DecorateNode(BTNode node, Type type) {
			m_createNodePosition = node.ChangeCoordinatesTo(this, new Vector2(Node.DEFAULT_WIDTH / 2f, -Node.DEFAULT_HEIGHT * 2.5f));

			int undoGroup = UndoUtilities.CreateGroup($"Decorate {node.Action.GetType().Name} with {type.Name}");

			BTNode decoratorNode = CreateNode(type);
			Decorator decorator = (Decorator)decoratorNode.Action;

			Connection toConnection = GetConnectionToNode(node);

			if (toConnection != null) {
				BTNode parentNode = toConnection.Start.Element.GetFirstOfType<BTNode>();

				Debug.Assert(parentNode.Action is Composite or Decorator);
				switch (parentNode.Action) {
					case Composite parentComposite:
						parentComposite.Children.Remove(node.Action);
						parentComposite.Children.Add(decorator);
						break;
					case Decorator parentDecorator:
						parentDecorator.Child = decorator;
						break;
				}

				toConnection.End = decoratorNode;
				((IConnectionSocket)node).IncomingConnections--;
				((IConnectionSocket)decoratorNode).IncomingConnections++;
			}

			decorator.Child = node.Action;
			AddConnection(new Connection(decoratorNode.OutSocket, node));

			Undo.CollapseUndoOperations(undoGroup);

			ApplyChanges();
		}

		private void ToggleBreakpoint(BTNode node) {
			Undo.RecordObject(m_behaviour, "Toggle node breakpoint");

			node.ToggleBreakpoint();
			ApplyChanges();
		}

		#endregion

		protected override void ApplyChanges() {
			m_behaviour.ConnectAllBlackboardRefs();

			base.ApplyChanges();
		}

		[CanBeNull]
		private BTNode GetNodeOfAction(BehaviourAction action) {
			foreach (BTNode node in m_nodes) {
				if (node.Action == action)
					return node;
			}

			return null;
		}

		[CanBeNull]
		private Connection GetConnectionToNode(BTNode node) {
			foreach (Connection connection in m_connections) {
				if (connection.End.Element == node)
					return connection;
			}

			return null;
		}

		private IList<Connection> GetConnectionsFromNode(BTNode node) {
			List<Connection> connections = null;

			foreach (Connection connection in m_connections) {
				if (connection.Start.Element == node.OutSocket) {
					connections ??= new List<Connection>();
					connections.Add(connection);
				}
			}

			return connections != null ? connections : Array.Empty<Connection>();
		}

		private List<BTNode> GetHierarchyOfNode(BTNode node) {
			List<BTNode> nodes = new();
			Inner(node, nodes);

			return nodes;

			void Inner(BTNode hierarchyNode, List<BTNode> acc) {
				acc.Add(hierarchyNode);

				BehaviourAction action = hierarchyNode.Action;

				if (action is Composite composite) {
					foreach (BehaviourAction child in composite.Children) {
						BTNode childNode = GetNodeOfAction(child);
						Inner(childNode, acc);
					}
				}
				else if (action is Decorator decorator) {
					if (decorator.Child == null) return;

					BTNode childNode = GetNodeOfAction(decorator.Child);
					Inner(childNode, acc);
				}
			}
		}
	}
}
