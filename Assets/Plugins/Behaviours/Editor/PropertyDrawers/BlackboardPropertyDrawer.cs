using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(Blackboard))]
	public class BlackboardPropertyDrawer : PropertyDrawer {
		internal static readonly IEnumerable<TypeProvider.SearchEntry> s_blackboardSearchTypes = TypeProvider.StandardTypes.Concat(TypeProvider.TypesToSearch(TypeCache.GetTypesWithAttribute(typeof(BehaviourTypeAttribute))));
		internal static BlackboardPropertyDrawer s_lastFocusedDrawer;
		private static int s_moveFrame;

		private SerializedProperty m_variablesProperty;
		private List<SerializedProperty> m_variableProperties = new();
		private List<Clickable> m_contextManipulators = new();

		private ListView m_listView;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();
			rootVisualElement.AddToClassList("BlackboardDrawer");

			VisualElement rowHeaderContainer = new VisualElement() { name = "RowHeader" };
			rowHeaderContainer.Add(new Label("Name"));
			rowHeaderContainer.Add(new Label("Value"));
			rootVisualElement.Add(rowHeaderContainer);

			m_variablesProperty = property.FindPropertyRelative(nameof(Blackboard.m_variables));
			ResetProperties();

			rootVisualElement.Add(m_listView = new ListView() {
				makeItem = MakeListItem,
				bindItem = BindListItem,
				unbindItem = UnbindListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				selectionType = SelectionType.None,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
			});
			m_listView.TrackPropertyValue(m_variablesProperty, OnPropertyChanged);
			m_listView.itemIndexChanged += OnVariableMoved;
			m_listView.RegisterCallback<FocusEvent, BlackboardPropertyDrawer>((_, self) => s_lastFocusedDrawer = self, this);

			// Assign after property tracker to ensure BlackboardVar's own tracker is after this one so their tracker invokes
			// after the blackboard. (Assigning the source creates and binds available items)
			// This is to let them handle if they have been detached from their panel
			m_listView.itemsSource = m_variableProperties;

			rootVisualElement.Add(new Button(CreateVariable) {
				name = "CreateButton",
				text = "Create Variable",
			});

			return rootVisualElement;
		}

		private void OnPropertyChanged(SerializedProperty _) {
			// Property was added or removed
			if (m_variablesProperty.arraySize != m_variableProperties.Count) {
				ResetProperties();
				m_listView.RefreshItems();
				return;
			}

			// Property was moved
			if (Time.frameCount == s_moveFrame || Time.frameCount == s_moveFrame + 1) {
				m_listView.RefreshItems();
				return;
			}

			// Property was changed in another drawer
			if (s_lastFocusedDrawer != this)
				m_listView.RefreshItems();
		}

		private void ResetProperties() {
			m_variableProperties.Clear();

			for (int i = 0; i < m_variablesProperty.arraySize; i++) {
				SerializedProperty variableProperty = m_variablesProperty.GetArrayElementAtIndex(i);
				m_variableProperties.Add(variableProperty);
			}
		}

		private VisualElement MakeListItem() {
			VisualElement root = new VisualElement() { name = "VariableRoot" };

			root.Add(new PropertyField());

			Image ctxButton = new Image() {
				name = "ContextButton",
				image = EditorGUIUtility.IconContent("_Menu@2x").image,
				scaleMode = ScaleMode.ScaleAndCrop,
			};
			root.Add(ctxButton);

			return root;
		}

		private void BindListItem(VisualElement element, int index) {
			if (m_contextManipulators.Count < index + 1)
				m_contextManipulators.Add(new Clickable(() => ShowVariableContext(index)));

			element.Q<PropertyField>().BindProperty(m_variableProperties[index]);
			element.Q<Image>("ContextButton").AddManipulator(m_contextManipulators[index]);
		}

		private void UnbindListItem(VisualElement element, int index) {
			element.Q<PropertyField>().Unbind();
			element.Q<Image>("ContextButton").RemoveManipulator(m_contextManipulators[index]);
		}

		private void ShowVariableContext(int index) {
			GenericMenu menu = new GenericMenu();
			Vector2 mousePosition = Event.current.mousePosition;

			menu.AddDisabledItem(new GUIContent(m_variableProperties[index].FindPropertyRelative("m_variableName").stringValue));
			menu.AddSeparator(string.Empty);

			menu.AddItem(new GUIContent("Change Type"), false, () => {
				TypeProvider.Instance.AskForType(GUIUtility.GUIToScreenPoint(mousePosition), s_blackboardSearchTypes, type => {
					ChangeVariableType(index, type);
				});
			});
			menu.AddSeparator(string.Empty);

			menu.AddItem(new GUIContent("Delete"), false, () => DeleteVariable(index));

			menu.ShowAsContext();
		}

		private void CreateVariable() {
			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

			TypeProvider.Instance.AskForType(mouseScreenPosition, s_blackboardSearchTypes, type => {
				int nextIndex = m_variableProperties.Count;

				m_variablesProperty.InsertArrayElementAtIndex(nextIndex);
				SerializedProperty newProperty = m_variablesProperty.GetArrayElementAtIndex(nextIndex);

				// Apply and update to ensure the guid's fixed buffer size is correct.
				// Not sure if it's a Unity bug or not but otherwise it reports a fixed buffer size of 0
				newProperty.serializedObject.ApplyModifiedProperties();
				newProperty.serializedObject.Update();

				// Set default values
				SerializedGUID guid = SerializedGUID.Generate();
				SerializedGUID.Editor_WriteToProperty(newProperty.FindPropertyRelative("m_guid"), guid);
				newProperty.FindPropertyRelative("m_guidString").stringValue = guid.ToString();
				newProperty.FindPropertyRelative("m_variableName").stringValue = $"new {type.Name} Variable";
				newProperty.FindPropertyRelative("m_serializedTypeName").stringValue = type.AssemblyQualifiedName;
				SetDefaultVariableValues(newProperty);
				newProperty.serializedObject.ApplyModifiedProperties();

				ResetProperties();
				m_listView.RefreshItems();

				// Focus on the just created variable's name for easy editing
				m_listView.GetRootElementForIndex(m_variableProperties.Count - 1).Q<TextField>().Focus();
			});
		}

		private static void SetDefaultVariableValues(SerializedProperty property) {
			property.FindPropertyRelative("m_boxedValue").managedReferenceValue = null;
			property.FindPropertyRelative("m_unityObjectValue").objectReferenceValue = null;
			property.FindPropertyRelative("m_primitiveValue").stringValue = null;
		}

		private void OnVariableMoved(int from, int to) {
			m_variablesProperty.MoveArrayElement(from, to);
			m_variablesProperty.serializedObject.ApplyModifiedProperties();

			s_moveFrame = Time.frameCount;

			ResetProperties();
			m_listView.RefreshItems();
		}

		internal void ChangeVariableType(int index, Type type) {
			SerializedProperty variableProperty = m_variableProperties[index];

			variableProperty.FindPropertyRelative("m_serializedTypeName").stringValue = type.AssemblyQualifiedName;
			SetDefaultVariableValues(variableProperty);

			m_variablesProperty.serializedObject.ApplyModifiedProperties();
			m_variablesProperty.serializedObject.Update();

			m_listView.RefreshItems();
		}

		internal void DeleteVariable(int index) {
			m_variableProperties.RemoveAt(index);
			m_variablesProperty.DeleteArrayElementAtIndex(index);
			m_variablesProperty.serializedObject.ApplyModifiedProperties();

			ResetProperties();
			m_listView.RefreshItems();
		}
	}
}
