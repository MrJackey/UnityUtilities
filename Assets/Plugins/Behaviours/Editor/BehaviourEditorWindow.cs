using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Editor.Graph;
using Jackey.Behaviours.Editor.Graph.BT;
using Jackey.Behaviours.Editor.Graph.FSM;
using Jackey.Behaviours.Editor.Utilities;
using Jackey.Behaviours.FSM;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Jackey.Behaviours.Editor {
	public class BehaviourEditorWindow : EditorWindow {
		[SerializeField] private StyleSheet m_graphStylesheet;
		[SerializeField] private StyleSheet m_blackboardStylesheet;
		[SerializeField] private StyleSheet m_validatorStylesheet;

		private ValidationPanel m_validationPanel;

		private Toolbar m_toolbar;
		private ToolbarBreadcrumbs m_toolbarBreadcrumbs;
		private Stack<ObjectBehaviour> m_depthStack = new();
		[CanBeNull]
		private BehaviourOwner m_depthOwner;

		[CanBeNull]
		private BehaviourGraph m_activeGraph;
		private BTGraph m_btGraph;
		private FSMGraph m_fsmGraph;

		[NonSerialized]
		private bool m_hasCreatedGUI;
		private bool m_isLocked;
		private bool m_isKeyUsed;

		public ObjectBehaviour OpenBehaviour => m_activeGraph?.Behaviour;

		[OnOpenAsset]
		private static bool OnOpenAsset(int instanceID) {
			Object assetToOpen = EditorUtility.InstanceIDToObject(instanceID);

			if (assetToOpen is ObjectBehaviour) {
				GetWindow<BehaviourEditorWindow>();
				return true;
			}

			return false;
		}

		[MenuItem("Tools/Jackey/Behaviours/Editor")]
		private static void ShowWindow() {
			BehaviourEditorWindow window = GetWindow<BehaviourEditorWindow>();
			window.Show();
		}

		private void OnEnable() {
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			Undo.undoRedoPerformed += OnUndoRedo;
		}

		private void OnDisable() {
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			Undo.undoRedoPerformed -= OnUndoRedo;
		}

		private void CreateGUI() {
			titleContent = new GUIContent("Behaviour Editor", EditorGUIUtility.IconContent("d_SceneViewTools").image);

			rootVisualElement.Add(m_toolbar = new Toolbar());
			m_toolbar.Add(m_toolbarBreadcrumbs = new ToolbarBreadcrumbs());
			m_toolbar.Add(new ToolbarSpacer() { flex = true });
			m_toolbar.Add(CreateShortcutMenu());

			m_validationPanel = new ValidationPanel(this);

			m_btGraph = new BTGraph();
			m_fsmGraph = new FSMGraph();

			rootVisualElement.styleSheets.Add(m_graphStylesheet);
			rootVisualElement.styleSheets.Add(m_blackboardStylesheet);
			rootVisualElement.styleSheets.Add(m_validatorStylesheet);

			EditorApplication.delayCall += OnSelectionChange;

			m_hasCreatedGUI = true;
		}

		private void RecreateGUI() {
			m_activeGraph = null;
			rootVisualElement.Clear();
			CreateGUI();
		}

		private void ShowButton(Rect rect) {
			m_isLocked = GUI.Toggle(rect, m_isLocked, GUIContent.none, "IN LockButton");
		}

		private void Update() {
			// Reset the window if the active behaviour has been destroyed
			if (m_activeGraph != null && m_activeGraph.Behaviour == null) {
				RecreateGUI();
				return;
			}

			m_activeGraph?.Tick();
		}

		private void OnGUI() {
			CheckInput();
		}

		private void OnDestroy() {
			m_btGraph?.SerializedBehaviour?.Dispose();
			m_fsmGraph?.SerializedBehaviour?.Dispose();
		}

		#region Input

		private ToolbarMenu CreateShortcutMenu() {
			ToolbarMenu shortcuts = new ToolbarMenu() { text = "Shortcuts" };

			shortcuts.menu.AppendAction(
				"(Space) Create Node",
				evt => m_activeGraph.BeginNodeCreation(EditorGUIUtility.ScreenToGUIPoint(position.center)),
				_ => m_activeGraph?.IsEditable ?? false ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
			);
			shortcuts.menu.AppendAction(
				"(Ctrl + D) Duplicate Selection",
				_ => m_activeGraph.DuplicateSelection(),
				_ => m_activeGraph?.IsEditable ?? false ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
			);
			shortcuts.menu.AppendAction(
				"(Ctrl + C) Copy Selection to Clipboard",
				_ => m_activeGraph.CopySelection(),
				_ => m_activeGraph?.IsEditable ?? false ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
				);
			shortcuts.menu.AppendAction(
				"(Ctrl + V) Paste from Clipboard",
				_ => m_activeGraph.Paste(EditorGUIUtility.ScreenToGUIPoint(position.center)),
				_ => m_activeGraph?.IsEditable ?? false ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
			);

			shortcuts.menu.AppendSeparator();

			shortcuts.menu.AppendAction(
				"(F) Frame Selection",
				_ => {
				if (m_activeGraph.SelectedElements.Count > 0)
					FrameSelection();
				else
					FrameContent();
				},
				_ => m_activeGraph != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
			);

			shortcuts.menu.AppendSeparator();
			shortcuts.menu.AppendAction(
				"(Del) Delete Selection",
				_ => m_activeGraph.DeleteSelection(),
				_ => m_activeGraph?.IsEditable ?? false ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
			);
			shortcuts.menu.AppendAction(
				"(Shift + Del) Smart Delete Selection",
				_ => m_activeGraph.SmartDeleteSelection(),
				_ => m_activeGraph?.IsEditable ?? false ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
			);

			shortcuts.menu.AppendSeparator();
			shortcuts.menu.AppendAction(
				"(Shift + R) Recreate GUI",
				_ => RecreateGUI()
			);

			return shortcuts;
		}

		private void CheckInput() {
			Event evt = Event.current;

			if (evt == null || m_activeGraph?.SerializedBehaviour == null)
				return;

			if (evt.type == EventType.KeyUp)
				m_isKeyUsed = false;

			if (evt.type == EventType.KeyDown && !m_isKeyUsed) {
				switch (evt.keyCode) {
					case KeyCode.Space:
						m_isKeyUsed = true;

						if (m_activeGraph.IsEditable)
							m_activeGraph.BeginNodeCreation(evt.mousePosition);

						break;
					case KeyCode.Delete when evt.shift:
						m_isKeyUsed = true;

						if (m_activeGraph.IsEditable)
							m_activeGraph.SmartDeleteSelection();

						break;
					case KeyCode.Delete:
						m_isKeyUsed = true;

						if (m_activeGraph.IsEditable)
							m_activeGraph.DeleteSelection();

						break;
					case KeyCode.R when evt.shift:
						m_isKeyUsed = true;

						RecreateGUI();

						break;
					case KeyCode.F:
						m_isKeyUsed = true;

						if (m_activeGraph.SelectedElements.Count > 0)
							FrameSelection();
						else
							FrameContent();

						break;
					case KeyCode.D when evt.control:
						m_isKeyUsed = true;

						if (m_activeGraph.IsEditable)
							m_activeGraph.DuplicateSelection();

						break;
					case KeyCode.C when evt.control:
						m_isKeyUsed = true;

						if (m_activeGraph.IsEditable)
							m_activeGraph.CopySelection();

						break;
					case KeyCode.V when evt.control:
						m_isKeyUsed = true;

						if (m_activeGraph.IsEditable)
							m_activeGraph.Paste(Event.current.mousePosition);

						break;
				}
			}
		}

		#endregion

		private void OnFocus() {
			m_isKeyUsed = false;
		}

		private void OnSelectionChange() {
			if (m_isLocked)
				return;

			if (Selection.count != 1)
				return;

			Object selectedObject = Selection.activeObject;

			if (selectedObject is ObjectBehaviour behaviour) {
				SetBehaviour(behaviour);
			}
			else if (selectedObject is GameObject selectedGameObject && selectedGameObject.TryGetComponent(out BehaviourOwner owner)) {
				SetOwnerBehaviour(owner);
			}
		}

		public void SetOwnerBehaviour(BehaviourOwner owner) {
			if (owner.Behaviour == null)
				return;

			// Don't consider owner in runtime as its blackboard merge downwards and will thus be shown with the graph
			m_depthOwner = EditorUtility.IsPersistent(owner.Behaviour) ? owner : null;

			// If the same as active graph, make sure that the owner blackboard is shown
			if (m_depthOwner != null && m_activeGraph != null && owner.Behaviour == m_activeGraph.SerializedBehaviour?.targetObject) {
				SetOwnerBlackboard();
				return;
			}

			while (m_toolbarBreadcrumbs.childCount > 0)
				m_toolbarBreadcrumbs.PopItem();

			m_depthStack.Clear();

			PushBehaviour(owner.Behaviour);
		}

		public void SetBehaviour(ObjectBehaviour behaviour) {
			if (behaviour == m_activeGraph?.SerializedBehaviour?.targetObject)
				return;

			while (m_toolbarBreadcrumbs.childCount > 0)
				m_toolbarBreadcrumbs.PopItem();

			m_depthStack.Clear();
			m_depthOwner = null;

			PushBehaviour(behaviour);
		}

		public void PushBehaviour(ObjectBehaviour behaviour) {
			if (behaviour == m_activeGraph?.SerializedBehaviour?.targetObject)
				return;

			int depthIndex = m_depthStack.Count;
			m_toolbarBreadcrumbs.PushItem(behaviour.name, () => GoToDepth(depthIndex));
			((IBindable)m_toolbarBreadcrumbs[depthIndex]).bindingPath = "m_Name";
			m_toolbarBreadcrumbs[depthIndex].Bind(new SerializedObject(behaviour));
			m_depthStack.Push(behaviour);

			EditBehaviour(behaviour);
		}

		private void EditBehaviour(ObjectBehaviour behaviour) {
			if (!IsBehaviourValid(behaviour)) {
				m_toolbar.visible = false;
				m_activeGraph?.RemoveFromHierarchy();
				m_activeGraph = null;

				if (m_validationPanel.parent == null)
					rootVisualElement.Add(m_validationPanel);

				m_validationPanel.Inspect(behaviour);
				return;
			}

			if (EditorUtility.IsPersistent(behaviour)) {
				if (!OnSelectedAsset(behaviour))
					return;
			}
			else {
				if (!OnSelectedInstance(behaviour))
					return;
			}

			m_toolbar.visible = true;

			Repaint();
			EditorApplication.delayCall += FrameContent;

			if (m_depthOwner != null)
				SetOwnerBlackboard();
		}

		private void SetOwnerBlackboard() {
			SerializedProperty blackboardProperty = new SerializedObject(m_depthOwner).FindProperty("m_blackboard");
			m_activeGraph!.BlackboardInspector.SetPrimaryBlackboard(m_depthOwner.Blackboard, blackboardProperty);
		}

		private void GoToDepth(int index) {
			Debug.Assert(index >= 0);
			Debug.Assert(index < m_depthStack.Count);

			while (m_toolbarBreadcrumbs.childCount > index + 1) {
				m_toolbarBreadcrumbs.PopItem();
				m_depthStack.Pop();
			}

			Debug.Assert(m_depthStack.Count == index + 1);
			EditBehaviour(m_depthStack.Peek());
		}

		private bool IsBehaviourValid(ObjectBehaviour behaviour) {
			if (SerializationUtility.HasManagedReferencesWithMissingTypes(behaviour))
				return false;

			switch (behaviour) {
				case BehaviourTree bt:
					if (bt.m_allActions.Any(action => action == null))
						return false;

					break;
			}

			return true;
		}

		private bool OnSelectedAsset(Object selectedObject) {
			if (selectedObject == m_activeGraph?.SerializedBehaviour?.targetObject)
				return false;

			if (selectedObject is not ObjectBehaviour selectedBehaviour)
				return false;

			ShowBehaviourGraph(selectedBehaviour);
			return true;
		}

		private bool OnSelectedInstance(Object selectedObject) {
			if (selectedObject == m_activeGraph?.SerializedBehaviour?.targetObject)
				return false;

			if (selectedObject is not ObjectBehaviour selectedBehaviour)
				return false;

			ShowBehaviourGraph(selectedBehaviour);
			return true;
		}

		private void ShowBehaviourGraph(ObjectBehaviour behaviour) {
			BehaviourGraph nextGraph = behaviour switch {
				BehaviourTree => m_btGraph,
				StateMachine => m_fsmGraph,
				_ => throw new ArgumentOutOfRangeException(nameof(behaviour), behaviour, null),
			};
			Debug.Assert(nextGraph != null);

			if (m_validationPanel.parent != null)
				m_validationPanel.RemoveFromHierarchy();

			if (m_activeGraph != null && m_activeGraph != nextGraph)
				m_activeGraph.RemoveFromHierarchy();

			m_activeGraph = nextGraph;
			m_activeGraph.BlackboardInspector.ClearBlackboards();

			if (m_activeGraph.parent == null)
				rootVisualElement.Add(m_activeGraph);

			m_activeGraph.UpdateBehaviour(behaviour);
		}

		#region Frame

		private void FrameSelection() {
			Debug.Assert(m_activeGraph?.SelectedElements.Count > 0);

			Rect? frame = null;

			foreach (ISelectableElement selected in m_activeGraph.SelectedElements) {
				VisualElement selectedElement = selected.Element;
				Rect selectedBound = selectedElement.hierarchy.parent.ChangeCoordinatesTo(m_activeGraph, selectedElement.localBound);
				frame ??= selectedBound;

				frame = frame.Value.Encapsulate(selectedBound);
			}

			Debug.Assert(frame != null);

			Vector2 graphSize = m_activeGraph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - frame.Value.center;

			m_activeGraph.contentContainer.transform.position += (Vector3)centerOffset;

			// TODO: Add zoom?
		}

		private void FrameContent() {
			Debug.Assert(m_activeGraph != null);

			if (m_activeGraph.childCount == 0) {
				m_activeGraph.contentContainer.transform.position = m_activeGraph.localBound.size / 2f;
				m_activeGraph.contentContainer.transform.scale = Vector3.one;
				return;
			}

			Rect? frame = null;

			foreach (VisualElement child in m_activeGraph.Children()) {
				Rect childBound = child.hierarchy.parent.ChangeCoordinatesTo(m_activeGraph, child.localBound);
				frame ??= childBound;

				frame = frame.Value.Encapsulate(childBound);
			}

			Debug.Assert(frame.HasValue);

			Vector2 graphSize = m_activeGraph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - frame.Value.center;

			m_activeGraph.contentContainer.transform.position += (Vector3)centerOffset;

			// TODO: Add zoom?
		}

		#endregion

		private void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
			// If viewing a runtime instance, clear everything as the instance is now destroyed
			if (stateChange == PlayModeStateChange.EnteredEditMode && m_activeGraph != null && m_activeGraph.Behaviour == null)
				RecreateGUI();

			// The window hasn't called CreateGUI, it's open but probably in a hidden tab
			if (!m_hasCreatedGUI)
				return;

			if (stateChange == PlayModeStateChange.EnteredEditMode)
				OnSelectionChange();
		}

		private void OnUndoRedo() {
			m_activeGraph?.UndoRedo();
		}
	}
}
