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
		private ListView m_repairListView;

		private ObjectBehaviour m_behaviour;
		private readonly List<RepairInfo> m_typeRepairs = new();

		private readonly List<EventCallback<ChangeEvent<string>>> m_updateRepairAssemblyActions = new();
		private readonly List<EventCallback<ChangeEvent<string>>> m_updateRepairNamespaceActions = new();
		private readonly List<EventCallback<ChangeEvent<string>>> m_updateRepairClassActions = new();
		private readonly List<Action> m_resetTypeRepairActions = new();

		public ValidationPanel(BehaviourEditorWindow window) {
			this.StretchToParentSize();

			m_window = window;

			Add(new Label("Validation Failed") { name = "Header" });

			Add(new Label("Missing Types") { name = "ListHeader" });
			Add(m_repairListView = new ListView() {
				itemsSource = m_typeRepairs,
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

			m_typeRepairs.Clear();
			foreach (ManagedReferenceMissingType missingType in SerializationUtility.GetManagedReferencesWithMissingTypes(behaviour)) {
				int index = m_typeRepairs.FindIndex(x => x.Assembly == missingType.assemblyName && x.Namespace == missingType.namespaceName && x.Class == missingType.className);
				if (index != -1) continue;

				m_typeRepairs.Add(new RepairInfo(missingType));
			}
			m_repairListView.RefreshItems();
		}

		private VisualElement MakeMissingTypeListItem() {
			VisualElement rootVisualElement = new VisualElement() { name = "MissingTypeItem"};

			VisualElement typeControls = new VisualElement() { name = "TypeControls" };

			VisualElement statusElement = new VisualElement() { name = "Status" };
			typeControls.Add(statusElement);

			Button searchButton = new Button(() => {
				TypeProvider.Instance.AskForType(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), GetAllSearchTypes(), type => {
					UQueryState<TextField> textFields = rootVisualElement.Query<TextField>().Build();
					textFields.AtIndex(0).value = type.Assembly.GetName().Name;
					textFields.AtIndex(1).value = type.Namespace;

					string className = type.Name;
					Type declaringType = type.DeclaringType;
					while (declaringType != null) {
						className = $"{declaringType.Name}/{className}";
						declaringType = declaringType.DeclaringType;
					}
					textFields.AtIndex(2).value = className;

					SetStatusClass(statusElement, "Valid");
				});
			}) { name = "Search" };
			searchButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Search Icon").image });
			typeControls.Add(searchButton);

			Button resetButton = new Button() { name = "Reset" };
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
			while (m_resetTypeRepairActions.Count <= index) {
				m_resetTypeRepairActions.Add(() => ResetMissingTypeListItem(index));
				m_updateRepairAssemblyActions.Add(evt => UpdateRepairAssembly(evt, index));
				m_updateRepairNamespaceActions.Add(evt => UpdateRepairNamespace(evt, index));
				m_updateRepairClassActions.Add(evt => UpdateRepairClass(evt, index));
			}

			RepairInfo repairInfo = m_typeRepairs[index];
			UQueryState<TextField> textFields = element.Query<TextField>().Build();

			TextField assemblyField = textFields.AtIndex(0);
			assemblyField.RegisterValueChangedCallback(m_updateRepairAssemblyActions[index]);
			assemblyField.SetValueWithoutNotify(repairInfo.Assembly);

			TextField namespaceField = textFields.AtIndex(1);
			namespaceField.RegisterValueChangedCallback(m_updateRepairNamespaceActions[index]);
			namespaceField.SetValueWithoutNotify(repairInfo.Namespace);

			TextField classField = textFields.AtIndex(2);
			classField.RegisterValueChangedCallback(m_updateRepairClassActions[index]);
			classField.SetValueWithoutNotify(repairInfo.Class);

			SetStatusClass(element.Q<VisualElement>("Status"), repairInfo.HasValidType ? "Valid" : "Missing");

			element.Q<Button>("Reset").clicked += m_resetTypeRepairActions[index];
		}

		private void UnbindMissingTypeListItem(VisualElement element, int index) {
			UQueryState<TextField> textFields = element.Query<TextField>().Build();
			textFields.AtIndex(0).UnregisterValueChangedCallback(m_updateRepairAssemblyActions[index]);
			textFields.AtIndex(1).UnregisterValueChangedCallback(m_updateRepairNamespaceActions[index]);
			textFields.AtIndex(2).UnregisterValueChangedCallback(m_updateRepairClassActions[index]);

			element.Q<Button>("Reset").clicked -= m_resetTypeRepairActions[index];
		}

		private void UpdateRepairAssembly(ChangeEvent<string> evt, int index) {
			RepairInfo repairInfo = m_typeRepairs[index];
			repairInfo.Assembly = evt.newValue;
			SetStatusClass(m_repairListView.GetRootElementForIndex(index).Q<VisualElement>("Status"), repairInfo.HasValidType ? "Valid" : "Unknown");
		}

		private void UpdateRepairNamespace(ChangeEvent<string> evt, int index) {
			RepairInfo repairInfo = m_typeRepairs[index];
			repairInfo.Namespace = evt.newValue;
			SetStatusClass(m_repairListView.GetRootElementForIndex(index).Q<VisualElement>("Status"), repairInfo.HasValidType ? "Valid" : "Unknown");
		}

		private void UpdateRepairClass(ChangeEvent<string> evt, int index) {
			RepairInfo repairInfo = m_typeRepairs[index];
			repairInfo.Class = evt.newValue;
			SetStatusClass(m_repairListView.GetRootElementForIndex(index).Q<VisualElement>("Status"), repairInfo.HasValidType ? "Valid" : "Unknown");
		}

		private void ResetMissingTypeListItem(int index) {
			VisualElement element = m_repairListView.GetRootElementForIndex(index);
			RepairInfo repairInfo = m_typeRepairs[index];

			UQueryState<TextField> textFields = element.Query<TextField>().Build();
			textFields.AtIndex(0).SetValueWithoutNotify(repairInfo.MissingType.assemblyName);
			textFields.AtIndex(1).SetValueWithoutNotify(repairInfo.MissingType.namespaceName);
			textFields.AtIndex(2).SetValueWithoutNotify(repairInfo.MissingType.className);
			repairInfo.Reset();

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
			foreach (RepairInfo repairInfo in m_typeRepairs) {
				SerializationUtilities.RepairMissingManagedTypes(
					m_behaviour,
					repairInfo.MissingType,
					repairInfo.Assembly,
					repairInfo.Namespace,
					repairInfo.Class
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

		private class RepairInfo {
			public readonly ManagedReferenceMissingType MissingType;
			public string Assembly;
			public string Namespace;
			public string Class;

			public bool HasValidType => Type.GetType($"{Namespace}.{Class}, {Assembly}") != null;

			public RepairInfo(ManagedReferenceMissingType missingType) {
				MissingType = missingType;

				Assembly = missingType.assemblyName;
				Namespace = missingType.namespaceName;
				Class = missingType.className;
			}

			public void Reset() {
				Assembly = MissingType.assemblyName;
				Namespace = MissingType.namespaceName;
				Class = MissingType.className;
			}
		}
	}
}
