using System;
using System.Linq;
using System.Reflection;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Operations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(OperationList))]
	public class OperationListPropertyDrawer : ManagedListPropertyDrawer<Operation> {
		internal static readonly Type[] s_operationTypes = TypeCache.GetTypesDerivedFrom<Operation>().Where(type => !type.IsAbstract).ToArray();

		protected override string CreateButtonText => "Add Operation";
		protected override Type[] CreateTypes => s_operationTypes;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();

			CreateListGUI(rootVisualElement, property.FindPropertyRelative("m_operations"));

			fieldInfo.GetCustomAttribute<CustomShowIfAttribute>()?.Bind(rootVisualElement, property);
			fieldInfo.GetCustomAttribute<CustomEnableIfAttribute>()?.Bind(rootVisualElement, property);

			return rootVisualElement;
		}
	}
}
