using Jackey.Behaviours.FSM;
using UnityEditor;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Editors {
	[CustomEditor(typeof(StateMachine))]
	public class StateMachineEditor : UnityEditor.Editor {
		public override VisualElement CreateInspectorGUI() {
			VisualElement rootVisualElement = new VisualElement();

			rootVisualElement.Add(new Button(OpenInEditor) {
				text = "Edit Behaviour",
			});

			return rootVisualElement;
		}

		private void OpenInEditor() {
			ObjectBehaviour assignedBehaviour = (ObjectBehaviour)serializedObject.targetObject;

			if (assignedBehaviour != null)
				EditorWindow.GetWindow<BehaviourEditorWindow>().SetBehaviour(assignedBehaviour);
		}
	}
}
