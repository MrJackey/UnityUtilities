using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class BlackboardInspector : VisualElement {
		public static Blackboard Blackboard;

		private PropertyField m_field;

		public override VisualElement contentContainer { get; }

		public BlackboardInspector() {
			style.position = Position.Absolute;
			style.maxHeight = Length.Percent(100f);

			hierarchy.Add(new Label("Blackboard") { name = "Header" });
			hierarchy.Add(m_field = new PropertyField());
			m_field.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			this.AddManipulator(new Dragger() { ConstrainToParent = true });
		}

		public void SetBlackboard(Blackboard blackboard, SerializedProperty property) {
			m_field.BindProperty(property);
			Blackboard = blackboard;
		}
	}
}
