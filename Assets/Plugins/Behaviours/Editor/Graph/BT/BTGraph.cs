using System;
using System.Collections.Generic;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.BT.Nested;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Editor.Utilities;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.BT {
	public class BTGraph : BehaviourGraph<BehaviourTree> {
		public BTGraph() {
			m_connectionManipulator.ConnectionVoided += OnConnectionVoided;
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionMoved += OnConnectionMoved;
			m_connectionManipulator.ConnectionRemoved += OnConnectionRemoved;
		}

		public void UpdateBehaviour(BehaviourTree behaviour) {
			m_serializedBehaviour?.Dispose();

			m_behaviour = behaviour;
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

		protected override void BuildGraph() {
			base.BuildGraph();

			foreach (BehaviourAction action in m_behaviour.m_allActions)
				AddNode(new BTNode(action));

			foreach (BehaviourAction action in m_behaviour.m_allActions) {
				BTNode node = GetNodeOfAction(action);

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
							start: GetNodeOfAction(decorator).OutSocket,
							end: GetNodeOfAction(decorator.Child)
						));
						break;
				}
			}

			foreach (ObjectBehaviour.EditorData.Group group in m_behaviour.Editor_Data.Groups)
				AddGroup(new GraphGroup(group.Rect));
		}

		#region Node CRUD

		public override void BeginNodeCreation() {
			base.BeginNodeCreation();

			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			TypeCache.TypeCollection actionTypes = TypeCache.GetTypesDerivedFrom<BehaviourAction>();

			TypeProvider.Instance.AskForType(mouseScreenPosition, actionTypes, type => CreateNode(type));
		}

		private BTNode CreateNode(Type type) {
			BehaviourAction action = (BehaviourAction)Activator.CreateInstance(type);
			BTNode node = CreateNode(action);

			Vector2 createPosition = this.ChangeCoordinatesTo(contentContainer, m_createNodePosition);
			createPosition.x -= Node.DEFAULT_WIDTH / 2f;
			createPosition.y -= Node.DEFAULT_HEIGHT / 2f;
			node.transform.position = createPosition;

			this.ReplaceSelection(node);

			return node;
		}

		public BTNode CreateNode(BehaviourAction action) {
			BTNode node = new BTNode(action);

			// TODO: Add undo
			m_behaviour.m_allActions.Add(action);
			m_serializedBehaviour.Update();

			AddNode(node);

			return node;
		}

		protected override void OnNodeAdded(Node node) {
			BTNode btNode = (BTNode)node;
			btNode.SetEntry(m_behaviour.m_entry == btNode.Action);

			btNode.AddManipulator(new ContextualMenuManipulator(ShowNodeContext));

			ConnectionSocket outSocket = btNode.OutSocket;
			outSocket.RegisterCallback<MouseDownEvent, IConnectionSocket>(OnSocketMouseDown, outSocket);
		}

		protected override void OnNodeRemoval(Node node) {
			BTNode btNode = (BTNode)node;
			BehaviourAction action = btNode.Action;

			if (action == m_behaviour.m_entry)
				m_behaviour.m_entry = null;

			for (int i = m_connections.Count - 1; i >= 0; i--) {
				Connection connection = m_connections[i];

				if (connection.Start.Element == btNode.OutSocket || connection.End.Element == node) {
					RemoveConnection(connection);
				}
			}

			bool done = false;
			foreach (BehaviourAction behaviourAction in m_behaviour.m_allActions) {
				switch (behaviourAction) {
					case Composite composite:
						done = composite.Children.Remove(action);
						break;
					case Decorator decorator:
						if (decorator.Child == action) {
							done = true;
							decorator.Child = null;
						}
						break;
				}

				if (done)
					break;
			}

			bool removed = m_behaviour.m_allActions.Remove(action);
			Debug.Assert(removed);

			m_serializedBehaviour.Update();
		}

		public override void DuplicateSelection() {
			if (SelectedElements.Count == 0) return;

			List<BTNode> originals = new List<BTNode>();
			List<BTNode> clones = new List<BTNode>();

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

			m_serializedBehaviour.Update();

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

		#endregion

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
					TypeCache.TypeCollection actionTypes = TypeCache.GetTypesDerivedFrom<Decorator>();

					TypeProvider.Instance.AskForType(mouseScreenPosition, actionTypes, type => DecorateNode(btNode, type));
				},
				editStatus
			);
			evt.menu.AppendAction(
				"Breakpoint",
				_ => { ToggleBreakpoint(btNode); },
				btNode.Action.Editor_Data.Breakpoint ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
			);

			evt.menu.AppendSeparator();
			evt.menu.AppendAction(
				"Delete",
				_ => {
					RemoveNode(btNode);
					m_serializedBehaviour.Update();
				},
				editStatus
			);
		}

		protected override void OnNodeDoubleClick(Node node) {
			BTNode btNode = (BTNode)node;

			if (btNode.Action is NestedBehaviourTree nestedTree && nestedTree.InstanceOrBehaviour != null) {
				EditorWindow.GetWindow<BehaviourEditorWindow>().PushBehaviour(nestedTree.InstanceOrBehaviour);
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

		#region Connection Callbacks

		private void OnConnectionVoided(Connection connection, Action<Connection, IConnectionSocket> restore) {
			SaveCreatePosition();

			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			TypeCache.TypeCollection actionTypes = TypeCache.GetTypesDerivedFrom<BehaviourAction>();

			TypeProvider.Instance.AskForType(mouseScreenPosition, actionTypes, type => {
				BTNode toNode = CreateNode(type);
				restore.Invoke(connection, toNode);
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

			m_serializedBehaviour.Update();
		}

		private void OnConnectionMoved(Connection connection, IConnectionSocket from, IConnectionSocket to) {
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
		}

		private void OnConnectionRemoved(Connection connection, IConnectionSocket start, IConnectionSocket end) {
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
		}

		#endregion

		#region Context Actions

		private void SetEntry(BTNode node) {
			if (m_behaviour.m_entry == node.Action)
				return;

			if (m_behaviour.m_entry != null) {
				BTNode oldStartNode = GetNodeOfAction(m_behaviour.m_entry);
				Debug.Assert(oldStartNode != null);

				oldStartNode.SetEntry(false);
			}

			m_behaviour.m_entry = node.Action;
			node.SetEntry(true);
			m_serializedBehaviour.Update();
		}

		private void DecorateNode(BTNode node, Type type) {
			m_createNodePosition = node.ChangeCoordinatesTo(this, new Vector2(Node.DEFAULT_WIDTH / 2f, -Node.DEFAULT_HEIGHT * 2.5f));

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

			m_serializedBehaviour.Update();
		}

		private void ToggleBreakpoint(BTNode node) {
			node.ToggleBreakpoint();
			m_serializedBehaviour.ApplyModifiedProperties();
		}

		#endregion

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
					connection.Add(connection);
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

		protected override void InspectElement(VisualElement element) {
			// TODO: Remove guard?
			if (m_behaviour == null)
				return;

			if (element is not BTNode node)
				return;

			int nodeIndex = m_behaviour.m_allActions.IndexOf(node.Action);

			Debug.Assert(nodeIndex != -1);

			SerializedProperty nodeProperty = m_serializedBehaviour.FindProperty($"{nameof(m_behaviour.m_allActions)}.Array.data[{nodeIndex}]");
			m_inspector.Inspect(node.Action.GetType(), nodeProperty);
		}
	}
}
