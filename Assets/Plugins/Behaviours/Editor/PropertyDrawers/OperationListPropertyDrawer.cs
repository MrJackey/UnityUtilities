using Jackey.Behaviours.Core.Operations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(OperationList))]
	public class OperationListPropertyDrawer : ManagedListPropertyDrawer<Operation> {
		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();

			CreateListGUI(rootVisualElement, property.FindPropertyRelative("m_operations"), "Add Operation");

			return rootVisualElement;
		}
	}
}
