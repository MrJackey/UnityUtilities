using System;
using System.Linq;
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

		protected override void SyncGraph() {
			base.SyncGraph();

			// Remove excess nodes
			for (int i = m_nodes.Count - 1; i >= 0; i--) {
				FSMNode node = (FSMNode)m_nodes[i];
				if (m_behaviour.m_allStates.Contains(node.State))
					continue;

				RemoveNode(node);
			}

			// Sync/add missing nodes
			foreach (BehaviourState state in m_behaviour.m_allStates) {
				FSMNode node = GetNodeOfState(state);

				if (node != null) {
					node.transform.position = state.Editor_Data.Position;
					node.SetEntry(m_behaviour.m_entry == state);
					continue;
				}

				AddNode(new FSMNode(state));
			}

			this.ValidateSelection();
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
			TypeProvider.Instance.AskForType(mouseScreenPosition, s_stateTypes, type => CreateNode(type));
		}

		private FSMNode CreateNode(Type type) {
			int undoGroup = UndoUtilities.CreateGroup($"Create {type.Name} node");
			Undo.RecordObject(m_behaviour, $"Create {type.Name} node");

			BehaviourState state = (BehaviourState)Activator.CreateInstance(type);
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
		}

		public override void DeleteNode(Node node) {
			FSMNode fsmNode = (FSMNode)node;
			BehaviourState state = fsmNode.State;

			Undo.RecordObject(m_behaviour, $"Delete {state.GetType().Name} node");

			if (state == m_behaviour.m_entry)
				m_behaviour.m_entry = null;

			bool removed = m_behaviour.m_allStates.Remove(state);
			Debug.Assert(removed);

			ApplyChanges();

			RemoveNode(node);
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
