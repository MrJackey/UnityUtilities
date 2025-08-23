using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.BT.Actions;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.BT.Nested;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Operations;
using Jackey.Behaviours.Editor.PropertyDrawers;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Editor.Utilities;
using Jackey.Behaviours.FSM;
using Jackey.Behaviours.FSM.States;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.FSM {
	public class FSMGraph : BehaviourGraph<StateMachine> {
		internal static readonly Type[] s_stateTypes = TypeCache.GetTypesDerivedFrom<BehaviourState>().Where(type => !type.IsAbstract).ToArray();
		internal static readonly Type[] s_actionTypes = TypeCache.GetTypesDerivedFrom<BehaviourAction>()
			.Where(type =>
				!type.IsAbstract && !typeof(Decorator).IsAssignableFrom(type) && !typeof(Composite).IsAssignableFrom(type) &&
				type != typeof(NestedBehaviourAction) && type != typeof(Operator))
			.ToArray();

		public FSMGraph() {
			m_connectionManipulator.ConnectionVoided += OnConnectionVoided;
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionMoved += OnConnectionMoved;
			m_connectionManipulator.ConnectionRemoved += OnConnectionRemoved;
		}

		protected override void SyncGraph() {
			base.SyncGraph();

			// Remove excess nodes
			for (int i = m_nodes.Count - 1; i >= 0; i--) {
				FSMNode node = (FSMNode)m_nodes[i];
				if (m_behaviour.m_allStates.Contains(node.State))
					continue;

				RemoveNode(node);
			}

			// Add missing nodes
			foreach (BehaviourState state in m_behaviour.m_allStates) {
				if (GetNodeOfState(state) != null)
					continue;

				AddNode(new FSMNode(state));
			}

			// Sync connections by removing and then recreating them
			RemoveAllConnections();

			foreach (BehaviourState state in m_behaviour.m_allStates) {
				FSMNode node = GetNodeOfState(state);
				Debug.Assert(node != null);

				node.transform.position = state.Editor_Data.Position;
				node.SetEntry(m_behaviour.m_entry == state);

				foreach (StateTransition transition in state.Transitions) {
					FSMNode destinationNode = GetNodeOfState(transition.Destination);

					AddConnection(new Connection(node.OutSockets[0], destinationNode));
				}
			}

			this.ValidateSelection();
		}

		public override void Tick() {
			foreach (Connection connection in m_connections) {
				if (connection.Start == null || connection.End == null) continue;

				FSMNode node = connection.Start.Element.GetFirstOfType<FSMNode>();
				node.MoveConnectionStartToClosestSocket(connection);
			}

			base.Tick();
		}

		protected override void UpdateEditorData() {
			base.UpdateEditorData();

			foreach (FSMNode node in m_nodes) {
				node.UpdateEditorData();
			}
		}

		#region Node CRUD

		public override void BeginNodeCreation(Vector2 GUIPosition) {
			base.BeginNodeCreation(GUIPosition);

			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(GUIPosition);
			IEnumerable<TypeProvider.SearchEntry> searchTypes = TypeProvider.TypesToSearch(s_stateTypes).Select(entry => { entry.Path = $"States/{entry.Path}"; return entry; })
				.Concat(TypeProvider.TypesToSearch(s_actionTypes).Select(entry => { entry.Path = $"Actions/{entry.Path}"; return entry; }))
				.Concat(TypeProvider.TypesToSearch(OperationListPropertyDrawer.s_operationTypes).Select(entry => { entry.Path = $"Operations/{entry.Path}"; return entry; }));
			TypeProvider.Instance.AskForType(mouseScreenPosition, searchTypes, type => CreateNode(type));
		}

		private FSMNode CreateNode(Type type) {
			int undoGroup = UndoUtilities.CreateGroup($"Create {type.Name} node");
			Undo.RecordObject(m_behaviour, $"Create {type.Name} node");

			BehaviourState state;
			if (typeof(BehaviourAction).IsAssignableFrom(type)) {
				ActionState actionState = new ActionState();
				actionState.SetAction((BehaviourAction)Activator.CreateInstance(type));
				state = actionState;
			}
			else if (typeof(Operation).IsAssignableFrom(type)) {
				OperationState operationState = new OperationState();
				operationState.Operations.Add((Operation)Activator.CreateInstance(type));
				state = operationState;
			}
			else {
				state = (BehaviourState)Activator.CreateInstance(type);
			}

			FSMNode node = new FSMNode(state);
			m_behaviour.m_allStates.Add(state);

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
			FSMNode fsmNode = (FSMNode)node;
			fsmNode.SetEntry(m_behaviour.m_entry == fsmNode.State);

			fsmNode.AddManipulator(new ContextualMenuManipulator(ShowNodeContext));

			if (m_isEditable) {
				foreach (IConnectionSocket outSocket in fsmNode.OutSockets) {
					outSocket.Element.RegisterCallback<MouseDownEvent, IConnectionSocket>(OnSocketMouseDown, outSocket);
				}
			}
		}

		public override void DeleteNode(Node node) {
			FSMNode fsmNode = (FSMNode)node;
			BehaviourState state = fsmNode.State;

			Undo.RecordObject(m_behaviour, $"Delete {state.GetType().Name} node");

			if (state == m_behaviour.m_entry)
				m_behaviour.m_entry = null;

			// Remove transitions to deleted state
			foreach (BehaviourState other in m_behaviour.m_allStates) {
				for (int i = other.Transitions.Count - 1; i >= 0; i--) {
					if (other.Transitions[i].Destination != state) continue;

					other.Transitions.RemoveAt(i);
				}
			}

			bool removed = m_behaviour.m_allStates.Remove(state);
			Debug.Assert(removed);

			ApplyChanges();

			RemoveNode(node);
		}

		protected override void OnNodeRemoval(Node node) {
			FSMNode fsmNode = (FSMNode)node;

			for (int i = m_connections.Count - 1; i >= 0; i--) {
				Connection connection = m_connections[i];

				if (fsmNode.OutSockets.Contains((IConnectionSocket)connection.Start.Element) || connection.End.Element == node) {
					RemoveConnection(connection);
				}
			}
		}

		#endregion

		private void OnSocketMouseDown(MouseDownEvent evt, IConnectionSocket socket) {
			evt.StopImmediatePropagation();

			if (socket.MaxOutgoingConnections != -1 && socket.OutgoingConnections >= socket.MaxOutgoingConnections)
				return;

			m_connectionManipulator.CreateConnection(socket);
		}

		protected override void InspectElement(VisualElement element) {
			if (element is not FSMNode node)
				return;

			int nodeIndex = m_behaviour.m_allStates.IndexOf(node.State);

			Debug.Assert(nodeIndex != -1);

			SerializedProperty nodeProperty = m_serializedBehaviour.FindProperty($"{nameof(m_behaviour.m_allStates)}.Array.data[{nodeIndex}]");
			m_inspector.Inspect(node.State.GetType(), nodeProperty);
		}

		#region Connection Callbacks

		private void OnConnectionVoided(Connection connection, Action<Connection, IConnectionSocket> restore) {
			SaveCreatePosition();

			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			TypeProvider.Instance.AskForType(mouseScreenPosition, s_stateTypes, type => {
				int undoGroup = UndoUtilities.CreateGroup($"Create {type.Name} node");

				FSMNode toNode = CreateNode(type);
				restore.Invoke(connection, toNode);

				Undo.CollapseUndoOperations(undoGroup);
			});
		}

		protected override bool IsConnectionValid(IConnectionSocket start, IConnectionSocket end) {
			FSMNode startNode = start.Element.GetFirstOfType<FSMNode>();
			FSMNode endNode = end.Element.GetFirstOfType<FSMNode>();

			// Prevent connecting to self
			if (startNode == endNode)
				return false;

			// Prevent multiple connections with same start and end.
			foreach (StateTransition transition in startNode.State.Transitions) {
				if (transition.Destination == endNode.State)
					return false;
			}

			return true;
		}

		private void OnConnectionCreated(Connection connection) {
			Undo.RecordObject(m_behaviour, "Create connection");

			FSMNode start = connection.Start.Element.GetFirstOfType<FSMNode>();
			FSMNode end = connection.End.Element.GetFirstOfType<FSMNode>();

			start.State.Transitions.Add(new StateTransition() { Destination = end.State });

			ApplyChanges();
		}

		private void OnConnectionMoved(Connection connection, IConnectionSocket from, IConnectionSocket to) {
			Undo.RecordObject(m_behaviour, "Move connection");

			FSMNode fromNode = from.Element.GetFirstOfType<FSMNode>();
			FSMNode toNode = to.Element.GetFirstOfType<FSMNode>();
			Debug.Assert(fromNode != null && toNode != null);

			bool movedStart = connection.Start == to;

			if (movedStart) {
				FSMNode endNode = connection.End.Element.GetFirstOfType<FSMNode>();

				// Find transition
				int transitionIndex = fromNode.State.Transitions.FindIndex(transition => transition.Destination == endNode.State);
				Debug.Assert(transitionIndex != -1);
				StateTransition transition = fromNode.State.Transitions[transitionIndex];

				// Move to new state
				fromNode.State.Transitions.RemoveAt(transitionIndex);
				toNode.State.Transitions.Add(transition);

			}
			else { // Moved end
				FSMNode startNode = connection.Start.Element.GetFirstOfType<FSMNode>();

				// Change destination of the transition
				StateTransition transition = startNode.State.Transitions.First(transition => transition.Destination == fromNode.State);
				transition.Destination = toNode.State;
			}

			ApplyChanges();
		}

		private void OnConnectionRemoved(Connection connection, IConnectionSocket start, IConnectionSocket end) {
			Undo.RecordObject(m_behaviour, "Delete connection");

			FSMNode endNode = end.Element.GetFirstOfType<FSMNode>();
			Debug.Assert(endNode != null);

			foreach (FSMNode node in m_nodes) {
				if (!node.OutSockets.Contains(start)) continue;

				for (int i = 0; i < node.State.Transitions.Count; i++) {
					if (node.State.Transitions[i].Destination != endNode.State) continue;

					node.State.Transitions.RemoveAt(i);
					break;
				}
			}

			m_connections.Remove(connection);
			ApplyChanges();
		}

		#endregion

		#region Context Menu

		private void ShowNodeContext(ContextualMenuPopulateEvent evt) {
			FSMNode fsmNode = (FSMNode)evt.target;
			DropdownMenuAction.Status editStatus = m_isEditable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

			evt.menu.AppendAction(
				"Entry",
				_ => { SetEntry(fsmNode); },
				m_behaviour.m_entry == fsmNode.State ? DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled : editStatus
			);
			evt.menu.AppendAction(
				"Breakpoint",
				_ => { ToggleBreakpoint(fsmNode); },
				fsmNode.State.Editor_Data.Breakpoint ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
			);

			MonoScript script = AssetUtilities.GetScriptAsset(fsmNode.State.GetType());
			if (script != null) {
				evt.menu.AppendSeparator();
				evt.menu.AppendAction(
					"Edit Script",
					_ => AssetDatabase.OpenAsset(script)
				);
			}

			evt.menu.AppendSeparator();
			evt.menu.AppendAction(
				"Delete",
				_ => {
					DeleteNode(fsmNode);
					this.ValidateSelection();
				},
				editStatus
			);
		}

		private void SetEntry(FSMNode node) {
			if (m_behaviour.m_entry == node.State)
				return;

			Undo.RecordObject(m_behaviour, $"Set {node.State.GetType().Name} as entry");

			if (m_behaviour.m_entry != null) {
				FSMNode oldStartNode = GetNodeOfState(m_behaviour.m_entry);
				Debug.Assert(oldStartNode != null);

				oldStartNode.SetEntry(false);
			}

			m_behaviour.m_entry = node.State;
			node.SetEntry(true);

			ApplyChanges();
		}

		private void ToggleBreakpoint(FSMNode node) {
			Undo.RecordObject(m_behaviour, "Toggle node breakpoint");

			node.ToggleBreakpoint();
			ApplyChanges();
		}

		#endregion

		[CanBeNull]
		private FSMNode GetNodeOfState(BehaviourState state) {
			foreach (FSMNode node in m_nodes) {
				if (node.State == state)
					return node;
			}

			return null;
		}
	}
}
