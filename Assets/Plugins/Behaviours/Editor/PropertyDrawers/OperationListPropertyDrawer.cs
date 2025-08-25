using System;
using System.Linq;
using System.Reflection;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Operations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(OperationList))]
	public class OperationListPropertyDrawer : PropertyDrawer {
		internal static readonly Type[] s_operationTypes = TypeCache.GetTypesDerivedFrom<Operation>().Where(type => !type.IsAbstract).ToArray();

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();

			rootVisualElement.Add(new ManagedListPropertyDrawer(
				property.FindPropertyRelative("m_operations"),
				"Add Operation",
				s_operationTypes)
			);

			fieldInfo.GetCustomAttribute<CustomShowIfAttribute>()?.Bind(rootVisualElement, property);
			fieldInfo.GetCustomAttribute<CustomEnableIfAttribute>()?.Bind(rootVisualElement, property);

			return rootVisualElement;
		}
	}
}
