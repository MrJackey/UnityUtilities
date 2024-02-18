using System;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.TypeSearch;
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

			Clear();
			BuildGraph();

			m_graphHeader.Bind(m_serializedBehaviour);
			m_graphInstanceInfo.text = EditorUtility.IsPersistent(m_behaviour) ? "(Asset)" : "(Instance)";

			m_blackboardInspector.SetBlackboard(behaviour.Blackboard, m_serializedBehaviour.FindProperty(nameof(ObjectBehaviour.m_blackboard)));
		}

		private void BuildGraph() {
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
		}

		public override void BeginNodeCreation() {
			base.BeginNodeCreation();

			TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<BehaviourAction>();
			TypeProvider.Instance.AskForType(types, type => CreateNode(type));
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

		private void ShowNodeContext(ContextualMenuPopulateEvent evt) {
			BTNode btNode = (BTNode)evt.target;

			evt.menu.AppendAction(
				"Entry",
				_ => { SetEntry(btNode); },
				m_behaviour.m_entry == btNode.Action ? DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal
			);
			evt.menu.AppendAction(
				"Breakpoint",
				_ => { ToggleBreakpoint(btNode); },
				btNode.Action.Editor_Data.Breakpoint ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
			);

			evt.menu.AppendSeparator();
			evt.menu.AppendAction("Delete", _ => {
				RemoveNode(btNode);
				m_serializedBehaviour.Update();
			});
		}

		private void OnSocketMouseDown(MouseDownEvent evt, IConnectionSocket socket) {
			evt.StopImmediatePropagation();

			if (socket.MaxOutgoingConnections != -1 && socket.OutgoingConnections >= socket.MaxOutgoingConnections)
				return;

			m_connectionManipulator.CreateConnection(socket);
		}

		private void OnConnectionVoided(Connection connection, Action<Connection, IConnectionSocket> restore) {
			SaveCreatePosition();

			TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<BehaviourAction>();

			TypeProvider.Instance.AskForType(types, type => {
				BTNode toNode = CreateNode(type);
				restore.Invoke(connection, toNode);
			});
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

		private void SetEntry(BTNode btNode) {
			if (m_behaviour.m_entry == btNode.Action)
				return;

			if (m_behaviour.m_entry != null) {
				BTNode oldStartNode = GetNodeOfAction(m_behaviour.m_entry);
				Debug.Assert(oldStartNode != null);

				oldStartNode.SetEntry(false);
			}

			m_behaviour.m_entry = btNode.Action;
			btNode.SetEntry(true);
			m_serializedBehaviour.Update();
		}

		private void ToggleBreakpoint(BTNode btNode) {
			btNode.ToggleBreakpoint();
			m_serializedBehaviour.ApplyModifiedProperties();
		}

		[CanBeNull]
		private BTNode GetNodeOfAction(BehaviourAction action) {
			foreach (BTNode node in m_nodes) {
				if (node.Action == action)
					return node;
			}

			return null;
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
			m_actionInspector.Inspect(node.Action.GetType(), nodeProperty);
		}
	}
}
