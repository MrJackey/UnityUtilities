using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class BlackboardInspector : VisualElement {
		private PropertyField m_primaryBlackboardField;
		private PropertyField m_secondaryBlackboardField;

		public override VisualElement contentContainer { get; }

		public BlackboardInspector() {
			style.position = Position.Absolute;
			style.maxHeight = Length.Percent(100f);

			hierarchy.Add(new Label("Blackboard") { name = "Header" });
			hierarchy.Add(m_primaryBlackboardField = new PropertyField());
			hierarchy.Add(m_secondaryBlackboardField = new PropertyField() {
				name = "SecondaryBlackboard",
			});

			m_primaryBlackboardField.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());
			m_secondaryBlackboardField.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			this.AddManipulator(new Dragger() { ConstrainToParent = true });
		}

		public void SetPrimaryBlackboard(Blackboard blackboard, SerializedProperty property) {
			m_primaryBlackboardField.BindProperty(property);
			Blackboard.Available[0] = blackboard;
		}

		public void SetSecondaryBlackboard(Blackboard blackboard, SerializedProperty property) {
			m_secondaryBlackboardField.BindProperty(property);
			Blackboard.Available[1] = blackboard;
		}

		public void ClearBlackboards() {
			m_primaryBlackboardField.Unbind();
			m_primaryBlackboardField.Clear();

			m_secondaryBlackboardField.Unbind();
			m_secondaryBlackboardField.Clear();

			for (int i = 0; i < Blackboard.Available.Length; i++)
				Blackboard.Available[i] = null;
		}
	}
}
