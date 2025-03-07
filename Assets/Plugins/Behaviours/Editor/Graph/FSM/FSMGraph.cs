using System;
using Jackey.Behaviours.Editor.Utilities;
using Jackey.Behaviours.FSM;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.FSM {
	public class FSMGraph : BehaviourGraph<StateMachine> {
		public FSMGraph() {
			m_connectionManipulator.ConnectionVoided += OnConnectionVoided;
			m_connectionManipulator.ConnectionCreated += OnConnectionCreated;
			m_connectionManipulator.ConnectionMoved += OnConnectionMoved;
			m_connectionManipulator.ConnectionRemoved += OnConnectionRemoved;
		}

		protected override void BuildGraph() {
			base.BuildGraph();

			foreach (BehaviourState state in m_behaviour.m_allStates)
				AddNode(new FSMNode(state));

			foreach (ObjectBehaviour.EditorData.Group group in m_behaviour.Editor_Data.Groups)
				AddGroup(new GraphGroup(group.Rect));
		}

		#region Node CRUD

		public override void BeginNodeCreation(Vector2 GUIPosition) {
			base.BeginNodeCreation(GUIPosition);
			CreateNode();
		}

		private FSMNode CreateNode() {
			BehaviourState state = new BehaviourState();
			FSMNode node = new FSMNode(state);

			// TODO: Add undo
			m_behaviour.m_allStates.Add(state);
			m_serializedBehaviour.Update();

			AddNode(node);
			node.transform.position = GetNodeCreatePosition();

			this.ReplaceSelection(node);

			return node;
		}

		protected override void OnNodeAdded(Node node) {
			FSMNode fsmNode = (FSMNode)node;
			fsmNode.SetEntry(fsmNode.State == m_behaviour.m_entry);

			fsmNode.AddManipulator(new ContextualMenuManipulator(ShowNodeContext));
		}

		protected override void OnNodeRemoval(Node node) {
			FSMNode fsmNode = (FSMNode)node;
			BehaviourState state = fsmNode.State;

			if (state == m_behaviour.m_entry)
				m_behaviour.m_entry = null;

			bool removed = m_behaviour.m_allStates.Remove(state);
			Debug.Assert(removed);

			m_serializedBehaviour.Update();
		}

		#endregion

		#region Connection Callbacks

		// TODO: Implement after figuring out transitions
		private void OnConnectionVoided(Connection connection, Action<Connection, IConnectionSocket> restore) { }
		private void OnConnectionCreated(Connection connection) { }
		private void OnConnectionMoved(Connection connection, IConnectionSocket from, IConnectionSocket to) { }
		private void OnConnectionRemoved(Connection connection, IConnectionSocket start, IConnectionSocket end) { }

		#endregion

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
					RemoveNode(fsmNode);
					m_serializedBehaviour.Update();
				},
				editStatus
			);
		}

		#region Context Actions

		private void SetEntry(FSMNode node) {
			if (m_behaviour.m_entry == node.State)
				return;

			if (m_behaviour.m_entry != null) {
				FSMNode oldStartNode = GetNodeOfState(m_behaviour.m_entry);
				Debug.Assert(oldStartNode != null);

				oldStartNode.SetEntry(false);
			}

			m_behaviour.m_entry = node.State;
			node.SetEntry(true);
			m_serializedBehaviour.Update();
		}

		private void ToggleBreakpoint(FSMNode node) {
			node.ToggleBreakpoint();
			m_serializedBehaviour.ApplyModifiedProperties();
		}

		#endregion

		protected override void InspectElement(VisualElement element) {
			if (element is not FSMNode node)
				return;

			int nodeIndex = m_behaviour.m_allStates.IndexOf(node.State);

			Debug.Assert(nodeIndex != -1);

			SerializedProperty nodeProperty = m_serializedBehaviour.FindProperty($"{nameof(m_behaviour.m_allStates)}.Array.data[{nodeIndex}]");
			m_inspector.Inspect(node.State.GetType(), nodeProperty);
		}

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
