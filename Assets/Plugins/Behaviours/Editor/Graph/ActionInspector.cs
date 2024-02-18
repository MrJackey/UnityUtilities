﻿using System;
using System.Linq;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.Manipulators;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class ActionInspector : VisualElement {
		private static readonly string[] s_hiddenProperties = { "Editor_Data", "m_target" };

		private Label m_header;

		public override VisualElement contentContainer { get; }

		public ActionInspector() {
			style.position = Position.Absolute;

			hierarchy.Add(m_header = new Label("Inspector"));
			hierarchy.Add(contentContainer = new VisualElement() {
				name = "Content",
				style = { display = DisplayStyle.None },
			});

			this.AddManipulator(new Dragger() { ConstrainToParent = true });
		}

		public void ClearInspection() {
			this.Unbind();

			Clear();

			m_header.text = "Inspector";
			contentContainer.style.display = DisplayStyle.None;
		}

		public void Inspect(Type type, SerializedProperty property) {
			Clear();

			m_header.text = ObjectNames.NicifyVariableName(type.Name);

			SerializedProperty targetProperty = property.FindPropertyRelative("m_target");
			if (targetProperty != null) {
				Add(new PropertyField(targetProperty) { name = "ActionTarget" });
			}

			int startDepth = property.depth;
			for (bool enterChildren = true; property.NextVisible(enterChildren) && property.depth > startDepth; enterChildren = false) {
				if (s_hiddenProperties.Contains(property.name)) continue;

				Add(new PropertyField(property));
			}

			this.Bind(property.serializedObject);

			contentContainer.style.display = contentContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}
