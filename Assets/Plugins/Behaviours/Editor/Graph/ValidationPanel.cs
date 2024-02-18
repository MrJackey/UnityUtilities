using Jackey.Behaviours.BT;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.Utilities;
using UnityEditor;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph {
	public class ValidationPanel : VisualElement {
		private BehaviourEditorWindow m_window;
		private ListView m_missingTypesList;

		private ObjectBehaviour m_behaviour;
		private ManagedReferenceMissingType[] m_missingTypes;

		public ValidationPanel(BehaviourEditorWindow window) {
			this.StretchToParentSize();

			m_window = window;

			Add(new Label("Validation Failed") { name = "Header" });

			Add(new Label("Missing Types") { name = "ListHeader" });
			Add(m_missingTypesList = new ListView() {
				makeItem = MakeMissingTypeListItem,
				bindItem = BindMissingTypeListItem,
				fixedItemHeight = 50f,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
			});

			VisualElement controlsRoot = new VisualElement() { name = "Controls" };
			controlsRoot.Add(new Button(Purge) { text = "Purge" });
			controlsRoot.Add(new Button(Repair) { text = "Repair" });
			Add(controlsRoot);
		}

		public void Inspect(ObjectBehaviour behaviour) {
			m_behaviour = behaviour;

			m_missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(behaviour);
			m_missingTypesList.itemsSource = m_missingTypes;
			m_missingTypesList.RefreshItems();
		}

		private VisualElement MakeMissingTypeListItem() {
			VisualElement rootVisualElement = new VisualElement() { name = "MissingTypeRoot" };

			rootVisualElement.Add(new TextField("Assembly")); // Assembly

			VisualElement typeRoot = new VisualElement() { name = "TypeRoot" };
			typeRoot.Add(new TextField("Namespace.Class")); // Namespace
			typeRoot.Add(new Label(".") { name = "TypeDeliminator" });
			typeRoot.Add(new TextField()); // Class
			rootVisualElement.Add(typeRoot);

			return rootVisualElement;
		}

		private void BindMissingTypeListItem(VisualElement element, int index) {
			ManagedReferenceMissingType missingType = m_missingTypes[index];

			element.Query<TextField>().AtIndex(0).SetValueWithoutNotify(missingType.assemblyName);
			element.Query<TextField>().AtIndex(1).SetValueWithoutNotify(missingType.namespaceName);
			element.Query<TextField>().AtIndex(2).SetValueWithoutNotify(missingType.className);
		}

		private void Purge() {
			SerializationUtility.ClearAllManagedReferencesWithMissingTypes(m_behaviour);
			AssetDatabase.Refresh();

			if (m_behaviour is BehaviourTree bt)
				CleanBehaviourTree(bt);

			AssetDatabase.Refresh();
			m_window.EditBehaviour(AssetDatabase.LoadAssetAtPath<ObjectBehaviour>(AssetDatabase.GetAssetPath(m_behaviour)));
		}

		private void CleanBehaviourTree(BehaviourTree bt) {
			for (int i = bt.m_allActions.Count - 1; i >= 0; i--) {
				BehaviourAction action = bt.m_allActions[i];

				switch (action) {
					case Composite composite:
						for (int childIndex = composite.Children.Count - 1; childIndex >= 0; childIndex--) {
							BehaviourAction child = composite.Children[childIndex];

							if (child == null)
								composite.Children.RemoveAt(childIndex);
						}

						break;
					case null:
						bt.m_allActions.RemoveAt(i);
						break;
				}
			}
		}

		private void Repair() {
			for (int i = 0; i < m_missingTypes.Length; i++) {
				VisualElement fieldsRoot = m_missingTypesList.GetRootElementForIndex(i);

				SerializationUtilities.RepairMissingManagedTypes(
					m_behaviour,
					m_missingTypes[i],
					fieldsRoot.Query<TextField>().AtIndex(0).value,
					fieldsRoot.Query<TextField>().AtIndex(1).value,
					fieldsRoot.Query<TextField>().AtIndex(2).value
				);
			}

			AssetDatabase.Refresh();
			m_window.EditBehaviour(AssetDatabase.LoadAssetAtPath<ObjectBehaviour>(AssetDatabase.GetAssetPath(m_behaviour)));
		}
	}
}
