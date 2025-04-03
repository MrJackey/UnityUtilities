using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Editors {
	[CustomEditor(typeof(BehaviourOwner))]
	public class BehaviourOwnerEditor : UnityEditor.Editor {
		[SerializeField] private StyleSheet m_styleSheet;

		public override VisualElement CreateInspectorGUI() {
			VisualElement rootVisualElement = new VisualElement();
			rootVisualElement.styleSheets.Add(m_styleSheet);

			rootVisualElement.Add(new Button(OpenInEditor) {
				text = "Edit Behaviour",
			});

			rootVisualElement.Add(new PropertyField(serializedObject.FindProperty("m_behaviour")));

			rootVisualElement.Add(new PropertyField(serializedObject.FindProperty("m_startMode")));
			rootVisualElement.Add(new PropertyField(serializedObject.FindProperty("m_updateMode")));
			rootVisualElement.Add(new PropertyField(serializedObject.FindProperty("m_repeatMode")));

			rootVisualElement.Add(new PropertyField(serializedObject.FindProperty("m_blackboard")) {
				name = "OwnerBlackboard",
			});

			return rootVisualElement;
		}

		private void OpenInEditor() {
			ObjectBehaviour assignedBehaviour = (ObjectBehaviour)serializedObject.FindProperty("m_behaviour").objectReferenceValue;

			if (assignedBehaviour != null)
				EditorWindow.GetWindow<BehaviourEditorWindow>().SetBehaviour(assignedBehaviour);
		}
	}
}
