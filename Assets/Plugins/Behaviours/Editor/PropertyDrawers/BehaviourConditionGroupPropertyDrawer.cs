using System.Reflection;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(BehaviourConditionGroup))]
	public class BehaviourConditionGroupPropertyDrawer : ManagedListPropertyDrawer<BehaviourCondition> {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();
			rootVisualElement.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

			SerializedProperty invertProperty = property.FindPropertyRelative("m_invert");
			rootVisualElement.Add(new PropertyField(invertProperty));

			SerializedProperty policyProperty = property.FindPropertyRelative("m_policy");
			rootVisualElement.Add(new PropertyField(policyProperty, string.Empty));

			CreateListGUI(rootVisualElement, property.FindPropertyRelative("m_conditions"), "Add Condition");

			fieldInfo.GetCustomAttribute<CustomShowIfAttribute>()?.Bind(rootVisualElement, property);
			fieldInfo.GetCustomAttribute<CustomEnableIfAttribute>()?.Bind(rootVisualElement, property);

			return rootVisualElement;
		}
	}
}
