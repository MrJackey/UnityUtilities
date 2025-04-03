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

				SerializationUtilities.RepairMissingManagedTypes(
					m_behaviour,
					m_missingTypes[i],
					fieldsRoot.Query<TextField>().AtIndex(0).value,
					fieldsRoot.Query<TextField>().AtIndex(1).value,
					fieldsRoot.Query<TextField>().AtIndex(2).value
				);
			}

			AssetDatabase.Refresh();
			m_window.SetBehaviour(LoadBehaviourAsset());
		}
	}
}
