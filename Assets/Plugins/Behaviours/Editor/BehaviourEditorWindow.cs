using System.Linq;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Editor.Graph;
using Jackey.Behaviours.Editor.Graph.BT;
using Jackey.Behaviours.Editor.Graph.FSM;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Jackey.Behaviours.Editor {
	public class BehaviourEditorWindow : EditorWindow {
		[SerializeField] private StyleSheet m_graphStylesheet;
		[SerializeField] private StyleSheet m_blackboardStylesheet;
		[SerializeField] private StyleSheet m_validatorStylesheet;

		private ValidationPanel m_validationPanel;

		private BehaviourGraph m_graph;
		private BTGraph m_btGraph;
		private FSMGraph m_fsmGraph;

		private bool m_isLocked;
		private bool m_isKeyUsed;

		public ObjectBehaviour OpenBehaviour => m_graph.Behaviour;

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
		}

		private void OnDisable() {
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void CreateGUI() {
			titleContent = new GUIContent("Behaviour Editor");

			m_validationPanel = new ValidationPanel(this);

			m_btGraph = new BTGraph();
			m_fsmGraph = new FSMGraph();

			rootVisualElement.styleSheets.Add(m_graphStylesheet);
			rootVisualElement.styleSheets.Add(m_blackboardStylesheet);
			rootVisualElement.styleSheets.Add(m_validatorStylesheet);

			EditorApplication.delayCall += OnSelectionChange;
		}

		private void ShowButton(Rect rect) {
			m_isLocked = GUI.Toggle(rect, m_isLocked, GUIContent.none, "IN LockButton");
		}

		private void Update() {
			m_graph?.Tick();
		}

		private void OnGUI() {
			CheckInput();
		}

		private void OnDestroy() {
			m_btGraph.SerializedBehaviour?.Dispose();
			m_fsmGraph.SerializedBehaviour?.Dispose();
		}

		private void CheckInput() {
			Event evt = Event.current;

			if (evt == null || m_graph?.SerializedBehaviour == null)
				return;

			if (evt.type == EventType.KeyUp)
				m_isKeyUsed = false;

			if (evt.type == EventType.KeyDown && !m_isKeyUsed) {
				switch (evt.keyCode) {
					case KeyCode.Space:
						m_isKeyUsed = true;
						m_graph.BeginNodeCreation();
						break;
					case KeyCode.Delete:
						m_isKeyUsed = true;
						DeleteSelectedElements();
						break;
					// TODO: Remove this
					case KeyCode.R when evt.shift:
						m_isKeyUsed = true;
						m_graph = null;
						rootVisualElement.Clear();
						CreateGUI();
						break;
					case KeyCode.F:
						m_isKeyUsed = true;
						if (m_graph.SelectedElements.Count > 0)
							FrameSelection();
						else
							FrameContent();
						break;
				}
			}
		}

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
				EditBehaviour(behaviour);
			}
			else if (selectedObject is GameObject selectedGameObject && selectedGameObject.TryGetComponent(out BehaviourOwner owner)) {
				if (owner.Behaviour == null)
					return;

				EditBehaviour(owner.Behaviour);
			}
		}

		public void EditBehaviour(ObjectBehaviour behaviour) {
			if (behaviour == m_graph?.SerializedBehaviour?.targetObject)
				return;

			if (!IsBehaviourValid(behaviour)) {
				m_graph?.RemoveFromHierarchy();
				m_graph = null;

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
			if (selectedObject == m_graph?.SerializedBehaviour?.targetObject)
				return false;

			switch (selectedObject) {
				case BehaviourTree bt:
					ChangeGraph(bt);
					break;
			}

			return true;
		}

		private bool OnSelectedInstance(Object selectedObject) {
			if (selectedObject == m_graph?.SerializedBehaviour?.targetObject)
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
			if (m_graph != null && m_graph != m_btGraph)
				m_graph.RemoveFromHierarchy();

			m_graph = m_btGraph;

			if (m_graph.parent == null)
				rootVisualElement.Add(m_graph);

			m_btGraph.UpdateBehaviour(bt);
		}

		private void DeleteSelectedElements() {
			if (m_graph.SelectedElements.Count == 0)
				return;

			// TODO: Add undo
			m_graph.ClearInspection();

			foreach (ISelectableElement selectedElement in m_graph.SelectedElements) {
				if (selectedElement.Element is Node node)
					m_graph.RemoveNode(node);
			}

			m_graph.SerializedBehaviour.Update();

			m_graph.SelectedElements.Clear();
			((ISelectionManager)m_graph).OnSelectionChange();
		}

		#region Frame

		private void FrameSelection() {
			Debug.Assert(m_graph.SelectedElements.Count > 0);

			Rect frame = m_graph.SelectedElements[0].Element.worldBound;

			for (int i = 1; i < m_graph.SelectedElements.Count; i++) {
				ISelectableElement selectedElement = m_graph.SelectedElements[i];
				Rect selectedBound = selectedElement.Element.worldBound;

				frame.xMin = Mathf.Min(frame.xMin, selectedBound.xMin);
				frame.xMax = Mathf.Max(frame.xMax, selectedBound.xMax);
				frame.yMin = Mathf.Min(frame.yMin, selectedBound.yMin);
				frame.yMax = Mathf.Max(frame.yMax, selectedBound.yMax);
			}

			Vector2 graphSize = m_graph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - frame.center;

			m_graph.contentContainer.transform.position += (Vector3)centerOffset;

			// TODO: Add zoom
		}

		private void FrameContent() {
			if (m_graph.childCount == 0) {
				m_graph.contentContainer.transform.position = m_graph.localBound.size / 2f;
				m_graph.contentContainer.transform.scale = Vector3.one;
				return;
			}

			Rect? frame = null;

			foreach (VisualElement child in m_graph.Children()) {
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

			Vector2 graphSize = m_graph.localBound.size;
			Vector2 centerOffset = graphSize / 2f - frame.Value.center;

			m_graph.contentContainer.transform.position += (Vector3)centerOffset;

			// TODO: Add zoom
		}

		#endregion

		private void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
			if (stateChange == PlayModeStateChange.EnteredEditMode)
				OnSelectionChange();
		}
	}
}
