using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Editor.Graph.BT;
using Jackey.Behaviours.Editor.PropertyDrawers;
using Jackey.Behaviours.Editor.TypeSearch;
using Jackey.Behaviours.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class ValidationPanel : VisualElement {
		private BehaviourEditorWindow m_window;
		private ListView m_missingTypesList;

		private ObjectBehaviour m_behaviour;
		private ManagedReferenceMissingType[] m_missingTypes;

		private readonly List<Action> m_resetMissingTypeActions = new();

		public ValidationPanel(BehaviourEditorWindow window) {
			this.StretchToParentSize();

			m_window = window;

			Add(new Label("Validation Failed") { name = "Header" });

			Add(new Label("Missing Types") { name = "ListHeader" });
			Add(m_missingTypesList = new ListView() {
				makeItem = MakeMissingTypeListItem,
				bindItem = BindMissingTypeListItem,
				unbindItem = UnbindMissingTypeListItem,
				fixedItemHeight = 70f,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
			});

			VisualElement controlsRoot = new VisualElement() { name = "Controls" };
			controlsRoot.Add(new Button(Purge) { name = "PurgeButton", text = "Purge" });
			controlsRoot.Add(new Button(Repair) { name = "RepairButton", text = "Repair" });
			Add(controlsRoot);
		}

		public void Inspect(ObjectBehaviour behaviour) {
			m_behaviour = behaviour;

			m_missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(behaviour);
			m_missingTypesList.itemsSource = m_missingTypes;
			m_missingTypesList.RefreshItems();
		}

		private VisualElement MakeMissingTypeListItem() {
			VisualElement rootVisualElement = new VisualElement() { name = "MissingTypeItem"};

			VisualElement typeControls = new VisualElement() { name = "TypeControls" };

			VisualElement statusElement = new VisualElement() { name = "Status" };
			typeControls.Add(statusElement);

			Button searchButton = new Button(() => {
				TypeProvider.Instance.AskForType(EditorGUIUtility.GUIToScreenPoint(Event.current.mousePosition), GetAllSearchTypes(), type => {
					UQueryState<TextField> textFields = rootVisualElement.Query<TextField>().Build();
					textFields.AtIndex(0).SetValueWithoutNotify(type.Assembly.GetName().Name);
					textFields.AtIndex(1).SetValueWithoutNotify(type.Namespace);
					textFields.AtIndex(2).SetValueWithoutNotify(type.Name);

					SetStatusClass(statusElement, "Valid");
				});
			}) { name = "Search" };
			searchButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Search Icon").image });
			typeControls.Add(searchButton);

			Button resetButton = new Button(() => {
				SetStatusClass(statusElement, "Missing");
			}) { name = "Reset" };
			resetButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Refresh").image });
			typeControls.Add(resetButton);

			rootVisualElement.Add(typeControls);

			VisualElement typeRoot = new VisualElement() { name = "MissingType" };
			TextField assemblyField = new TextField("Assembly:");
			assemblyField.RegisterValueChangedCallback(_ => SetStatusClass(statusElement, "Unknown"));
			typeRoot.Add(assemblyField);

			TextField namespaceField = new TextField("Namespace:");
			namespaceField.RegisterValueChangedCallback(_ => SetStatusClass(statusElement, "Unknown"));
			typeRoot.Add(namespaceField);

			TextField classField = new TextField("Class:");
			classField.RegisterValueChangedCallback(_ => SetStatusClass(statusElement, "Unknown"));
			typeRoot.Add(classField);

			rootVisualElement.Add(typeRoot);

			return rootVisualElement;
		}

		private void BindMissingTypeListItem(VisualElement element, int index) {
			ManagedReferenceMissingType missingType = m_missingTypes[index];

			UQueryState<TextField> textFields = element.Query<TextField>().Build();
			textFields.AtIndex(0).SetValueWithoutNotify(missingType.assemblyName);
			textFields.AtIndex(1).SetValueWithoutNotify(missingType.namespaceName);
			textFields.AtIndex(2).SetValueWithoutNotify(missingType.className);

			SetStatusClass(element.Q<VisualElement>("Status"), "Missing");

			if (index >= m_resetMissingTypeActions.Count)
				m_resetMissingTypeActions.Add(() => ResetMissingTypeListItem(index));

			element.Q<Button>("Reset").clicked += m_resetMissingTypeActions[index];
		}

		private void UnbindMissingTypeListItem(VisualElement element, int index) {
			element.Q<Button>("Reset").clicked -= m_resetMissingTypeActions[index];
		}

		private void ResetMissingTypeListItem(int index) {
			VisualElement element = m_missingTypesList.GetRootElementForIndex(index);
			ManagedReferenceMissingType missingType = m_missingTypes[index];

			UQueryState<TextField> textFields = element.Query<TextField>().Build();
			textFields.AtIndex(0).SetValueWithoutNotify(missingType.assemblyName);
			textFields.AtIndex(1).SetValueWithoutNotify(missingType.namespaceName);
			textFields.AtIndex(2).SetValueWithoutNotify(missingType.className);

			SetStatusClass(element.Q<VisualElement>("Status"), "Missing");
		}

		private void SetStatusClass(VisualElement element, string className) {
			element.RemoveFromClassList("Missing");
			element.RemoveFromClassList("Unknown");
			element.RemoveFromClassList("Valid");

			element.AddToClassList(className);
		}

		private void Purge() {
			SerializationUtility.ClearAllManagedReferencesWithMissingTypes(m_behaviour);

			EditorUtility.SetDirty(m_behaviour);
			AssetDatabase.SaveAssetIfDirty(m_behaviour);

			SerializationUtilities.RemoveNullManagedTypes(LoadBehaviourAsset());

			AssetDatabase.Refresh();
			m_window.SetBehaviour(LoadBehaviourAsset());
		}

		private ObjectBehaviour LoadBehaviourAsset() {
			return AssetDatabase.LoadAssetAtPath<ObjectBehaviour>(AssetDatabase.GetAssetPath(m_behaviour));
		}

		private void Repair() {
			for (int i = 0; i < m_missingTypes.Length; i++) {
				VisualElement fieldsRoot = m_missingTypesList.GetRootElementForIndex(i);
				UQueryState<TextField> textFields = fieldsRoot.Query<TextField>().Build();

				SerializationUtilities.RepairMissingManagedTypes(
					m_behaviour,
					m_missingTypes[i],
					textFields.AtIndex(0).value,
					textFields.AtIndex(1).value,
					textFields.AtIndex(2).value
				);
			}

			AssetDatabase.Refresh();
			m_window.SetBehaviour(LoadBehaviourAsset());
		}

		private IEnumerable<TypeProvider.SearchEntry> GetAllSearchTypes() {
			switch (m_behaviour) {
				case BehaviourTree:
					IEnumerable<TypeProvider.SearchEntry> actions = TypeProvider.TypesToSearch(BTGraph.s_actionTypes).Select(entry => {
						entry.Path = $"Actions/{entry.Path}";
						return entry;
					});
					IEnumerable<TypeProvider.SearchEntry> operations = TypeProvider.TypesToSearch(OperationListPropertyDrawer.s_operationTypes).Select(entry => {
						entry.Path = $"Operations/{entry.Path}";
						return entry;
					});
					IEnumerable<TypeProvider.SearchEntry> conditions = TypeProvider.TypesToSearch(BehaviourConditionGroupPropertyDrawer.s_conditionTypes).Select(entry => {
						entry.Path = $"Conditions/{entry.Path}";
						return entry;
					});
					return actions.Concat(operations).Concat(conditions);
			}

			return Enumerable.Empty<TypeProvider.SearchEntry>();
		}
	}
}
