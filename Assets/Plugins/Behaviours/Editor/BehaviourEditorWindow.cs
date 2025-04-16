using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Editor.Graph;
using Jackey.Behaviours.Editor.Graph.BT;
using Jackey.Behaviours.Editor.Graph.FSM;
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
		private BehaviourGraph m_activeGraph;
		private BTGraph m_btGraph;
		private FSMGraph m_fsmGraph;

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
			titleContent = new GUIContent("Behaviour Editor");

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
				if (owner.Behaviour == null)
					return;

				SetBehaviour(owner.Behaviour);

				// Prevent showing owner blackboard in runtime as the variables merge downwards and will thus be shown with the graph
				if (m_activeGraph != null && EditorUtility.IsPersistent(owner.Behaviour)) {
					SerializedProperty blackboardProperty = new SerializedObject(owner).FindProperty("m_blackboard");
					m_activeGraph.BlackboardInspector.SetPrimaryBlackboard(owner.Blackboard, blackboardProperty);
				}
			}
		}

		public void SetBehaviour(ObjectBehaviour behaviour) {
			if (behaviour == m_activeGraph?.SerializedBehaviour?.targetObject)
				return;

			while (m_toolbarBreadcrumbs.childCount > 0)
				m_toolbarBreadcrumbs.PopItem();

			m_depthStack.Clear();
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

			Repaint();
			EditorApplication.delayCall += FrameContent;
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

			switch (selectedObject) {
				case BehaviourTree bt:
					ChangeGraph(bt);
					break;
			}

			return true;
		}

		private bool OnSelectedInstance(Object selectedObject) {
			if (selectedObject == m_activeGraph?.SerializedBehaviour?.targetObject)
				return false;

			switch (selectedObject) {
				case BehaviourTree bt:
					ChangeGraph(bt);
					break;
			}

			return true;
		}

		// TODO: Implement
		// private void ChangeGraph(StateMachine fsm) { }
		private void ChangeGraph(BehaviourTree bt) {
			if (m_validationPanel.parent != null)
				m_validationPanel.RemoveFromHierarchy();

			if (m_activeGraph != null && m_activeGraph != m_btGraph)
				m_activeGraph.RemoveFromHierarchy();

			m_activeGraph = m_btGraph;
			m_activeGraph.BlackboardInspector.ClearBlackboards();

			if (m_activeGraph.parent == null)
				rootVisualElement.Add(m_activeGraph);

			m_btGraph.UpdateBehaviour(bt);
		}

		#region Frame

		private void FrameSelection() {
			Debug.Assert(m_activeGraph?.SelectedElements.Count > 0);

			Rect frame = m_activeGraph.SelectedElements[0].Element.worldBound;

			for (int i = 1; i < m_activeGraph.SelectedElements.Count; i++) {
				ISelectableElement selectedElement = m_activeGraph.SelectedElements[i];
				Rect selectedBound = selectedElement.Element.worldBound;

				frame.xMin = Mathf.Min(frame.xMin, selectedBound.xMin);
				frame.xMax = Mathf.Max(frame.xMax, selectedBound.xMax);
				frame.yMin = Mathf.Min(frame.yMin, selectedBound.yMin);
				frame.yMax = Mathf.Max(frame.yMax, selectedBound.yMax);
			}

			Vector2 graphSize = m_activeGraph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - frame.center;

			m_activeGraph.contentContainer.transform.position += (Vector3)centerOffset;

			// TODO: Add zoom
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
				Rect childBound = child.worldBound;
				frame ??= childBound;

				Rect rect = frame.Value;
				rect.xMin = Mathf.Min(rect.xMin, childBound.xMin);
				rect.xMax = Mathf.Max(rect.xMax, childBound.xMax);
				rect.yMin = Mathf.Min(rect.yMin, childBound.yMin);
				rect.yMax = Mathf.Max(rect.yMax, childBound.yMax);
				frame = rect;
			}

			Debug.Assert(frame.HasValue);

			Vector2 graphSize = m_activeGraph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - frame.Value.center;

			m_activeGraph.contentContainer.transform.position += (Vector3)centerOffset;

			// TODO: Add zoom
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
