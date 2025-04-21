using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class BlackboardInspector : VisualElement {
		private Label m_primaryLabel;
		private PropertyField m_primaryBlackboardField;
		private Label m_secondaryLabel;
		private PropertyField m_secondaryBlackboardField;

		public BlackboardInspector() {
			style.position = Position.Absolute;
			style.maxHeight = Length.Percent(100f);

			Add(new Label("Blackboard") { name = "Header" });
			Add(m_primaryLabel = new Label("Owner") { style = { display = DisplayStyle.None }});
			m_primaryLabel.AddToClassList("BlackboardHeader");
			Add(m_primaryBlackboardField = new PropertyField() { name = "PrimaryBlackboard" });

			Add(m_secondaryLabel = new Label("Graph") { style = { display = DisplayStyle.None }});
			m_secondaryLabel.AddToClassList("BlackboardHeader");
			Add(m_secondaryBlackboardField = new PropertyField());

			m_primaryBlackboardField.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());
			m_secondaryBlackboardField.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			this.AddManipulator(new Dragger() { ConstrainToParent = true });
		}

		public void SetPrimaryBlackboard(Blackboard blackboard, SerializedProperty property) {
			m_primaryLabel.style.display = DisplayStyle.Flex;
			m_primaryBlackboardField.style.display = DisplayStyle.Flex;
			m_primaryBlackboardField.BindProperty(property);
			Blackboard.Available[0] = blackboard;

			m_secondaryLabel.style.display = DisplayStyle.Flex;
		}

		public void SetSecondaryBlackboard(Blackboard blackboard, SerializedProperty property) {
			m_secondaryBlackboardField.style.display = DisplayStyle.Flex;
			m_secondaryBlackboardField.BindProperty(property);
			Blackboard.Available[1] = blackboard;
		}

		public void ClearBlackboards() {
			m_primaryLabel.style.display = DisplayStyle.None;
			m_primaryBlackboardField.Unbind();
			m_primaryBlackboardField.Clear();
			m_primaryBlackboardField.style.display = DisplayStyle.None;

			m_secondaryLabel.style.display = DisplayStyle.None;
			m_secondaryBlackboardField.Unbind();
			m_secondaryBlackboardField.Clear();
			m_secondaryBlackboardField.style.display = DisplayStyle.None;

			for (int i = 0; i < Blackboard.Available.Length; i++)
				Blackboard.Available[i] = null;
		}
	}
}
