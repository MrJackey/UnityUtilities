using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Editor.TypeSearch;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.PropertyDrawers {
	[CustomPropertyDrawer(typeof(Blackboard))]
	public class BlackboardPropertyDrawer : PropertyDrawer {
		private static BlackboardPropertyDrawer s_lastFocusedDrawer;
		private static int s_moveFrame;

		private SerializedProperty m_variablesProperty;
		private List<SerializedProperty> m_variableProperties = new();
		private List<Clickable> m_contextManipulators = new();

		private ListView m_listView;

		public override VisualElement CreatePropertyGUI(SerializedProperty property) {
			VisualElement rootVisualElement = new VisualElement();

			VisualElement rowHeaderContainer = new VisualElement() { name = "RowHeader" };
			rowHeaderContainer.Add(new Label("Name"));
			rowHeaderContainer.Add(new Label("Value"));
			rootVisualElement.Add(rowHeaderContainer);

			m_variablesProperty = property.FindPropertyRelative(nameof(Blackboard.m_variables));
			ResetProperties();

			rootVisualElement.Add(m_listView = new ListView(m_variableProperties) {
				makeItem = MakeListItem,
				bindItem = BindListItem,
				unbindItem = UnbindListItem,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				selectionType = SelectionType.None,
			});

			m_listView.RegisterCallback<FocusEvent, BlackboardPropertyDrawer>((_, self) => s_lastFocusedDrawer = self, this);
			m_listView.TrackPropertyValue(m_variablesProperty, OnPropertyChanged);
			m_listView.itemIndexChanged += OnVariableMoved;
			m_listView.schedule.Execute(() => m_listView.Focus());

			rootVisualElement.Add(new Button(CreateVariable) {
				name = "CreateButton",
				text = "Create Variable",
			});

			Undo.undoRedoPerformed += OnUndoRedo;

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

			menu.AddDisabledItem(new GUIContent(m_variableProperties[index].FindPropertyRelative("m_variableName").stringValue));
			menu.AddSeparator(string.Empty);

			menu.AddItem(new GUIContent("Delete"), false, () => {
					m_variableProperties.RemoveAt(index);
					m_variablesProperty.DeleteArrayElementAtIndex(index);
					m_variablesProperty.serializedObject.ApplyModifiedProperties();

					ResetProperties();
					m_listView.RefreshItems();
				}
			);

			menu.ShowAsContext();
		}

		private void CreateVariable() {
			Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

			TypeCache.TypeCollection userTypes = TypeCache.GetTypesWithAttribute(typeof(BehaviourTypeAttribute));
			IEnumerable<TypeProvider.SearchEntry> blackboardSearchTypes = TypeProvider.StandardTypes.Concat(TypeProvider.TypesToSearch(userTypes));

			TypeProvider.Instance.AskForType(mouseScreenPosition, blackboardSearchTypes, type => {
				int nextIndex = m_variableProperties.Count;

				m_variablesProperty.InsertArrayElementAtIndex(nextIndex);
				SerializedProperty newProperty = m_variablesProperty.GetArrayElementAtIndex(nextIndex);

				// Set default values
				newProperty.FindPropertyRelative("m_guid").stringValue = GUID.Generate().ToString();
				newProperty.FindPropertyRelative("m_variableName").stringValue = $"new {type.Name} Variable";
				newProperty.FindPropertyRelative("m_serializedTypeName").stringValue = type.AssemblyQualifiedName;
				newProperty.FindPropertyRelative("m_boxedValue").managedReferenceValue = null;
				newProperty.FindPropertyRelative("m_unityObjectValue").objectReferenceValue = null;
				newProperty.FindPropertyRelative("m_primitiveValue").stringValue = null;
				newProperty.serializedObject.ApplyModifiedProperties();

				ResetProperties();
				m_listView.RefreshItems();
			});
		}

		private void OnVariableMoved(int from, int to) {
			m_variablesProperty.MoveArrayElement(from, to);
			m_variablesProperty.serializedObject.ApplyModifiedProperties();

			s_moveFrame = Time.frameCount;

			ResetProperties();
			m_listView.RefreshItems();
		}

		private void OnUndoRedo() {
			ResetProperties();
			m_listView.RefreshItems();
		}
	}
}
