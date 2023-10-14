using System;
using UnityEngine;
using Component = UnityEngine.Component;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Jackey.HierarchyOrganizer.Runtime {
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[ExecuteAlways]
	internal class HierarchyFolder : MonoBehaviour {
#if UNITY_EDITOR
		private bool m_preventInitialization;
		private bool m_initializing;
		private bool m_initialized;

		private Transform m_preMoveParent;

		internal static event Action<HierarchyFolder> Initialized;

		// OnValidate is used to initialize the folder due to it being called when
		// the instance is created or playmode is entered regardless of its GameObject's active state.
		private void OnValidate() {
			hideFlags |= HideFlags.HideInInspector | HideFlags.NotEditable;
			transform.hideFlags |= HideFlags.HideInInspector | HideFlags.NotEditable;

			if (!m_initialized && !m_initializing && !m_preventInitialization) {
				// The delay is needed due to parent manipulation sending messages which
				// is not supported in OnValidate and therefore logs warnings
				EditorApplication.delayCall += Initialize;

				m_initializing = true;
			}
		}

		private void OnBeforeTransformParentChanged() {
			if (m_initialized) {
				m_preMoveParent = transform.parent;
			}
		}

		private void OnTransformParentChanged() {
			// Prevent childing a folder to a non-folder game object
			Transform parent = transform.parent;

			if (parent && !parent.TryGetComponent(out HierarchyFolder _)) {
				transform.SetParent(m_preMoveParent);

				// Handle if a folder somehow entered a prefab stage (e.g. if a user pastes an already created folder)
				PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

				if (prefabStage != null) {
					bool stageIsDirty = prefabStage.scene.isDirty;

					EditorUtility.DisplayDialog(
						"Hierarchy Organizer",
						"A folder should not be included in a prefab. They are only meant to be used within scenes",
						"Ok"
					);

					EditorApplication.delayCall += () => {
						DestroyImmediate(gameObject);

						if (!stageIsDirty)
							prefabStage.ClearDirtiness();
					};

					m_preventInitialization = true;
				}
			}
		}

		private void OnDestroy() {
			if (transform) {
				transform.hideFlags &= ~HideFlags.HideInInspector;
			}
		}

		internal void Initialize() {
			// Prevent initializing a folder which has been destroyed.
			// It happens when a nested folder is turned into a prefab and is
			// therefore deleted by its root folder
			if (!this) return;

			EditorApplication.delayCall -= Initialize;

			// Prevent creating a prefab of folders as they are only meant to be used in scenes
			if (PrefabUtility.IsPartOfPrefabAsset(gameObject) && !transform.parent) {
				PrefabUtility.UnpackAllInstancesOfPrefab(gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
				AssetDatabase.DeleteAsset(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject));
				Undo.RevertAllInCurrentGroup();

				EditorUtility.DisplayDialog(
					"Hierarchy Organizer",
					"A folder should not be in a prefab. They are only meant to be used in scenes",
					"Ok"
				);

				return;
			}

			// Doing this as OnDestroy is used in the component which requires the
			// GameObject to be active at least once
			EnsureGameObjectActivation();

			m_initialized = true;

			Initialized?.Invoke(this);
		}

		private void EnsureGameObjectActivation() {
			if (isActiveAndEnabled) return;

			Transform parent = transform.parent;
			int siblingIndex = transform.GetSiblingIndex();
			bool active = gameObject.activeSelf;

			transform.SetParent(null);
			gameObject.SetActive(true);

			transform.SetParent(parent);
			transform.SetSiblingIndex(siblingIndex);
			gameObject.SetActive(active);
		}

		internal int GetDepth() {
			int depth = 0;

			Transform ancestor = transform.parent;
			while (ancestor) {
				depth++;
				ancestor = ancestor.parent;
			}

			return depth;
		}

		#region Flatten

		internal void Flatten() {
			if (GetComponents<Component>().Length > 2)
				Debug.LogWarning($"Folder \"{name}\" in scene {gameObject.scene.name} has components on it which is lost during folder stripping", this);

			DetachChildren();
		}

		private void DetachChildren() {
			Transform myTransform = transform;
			Transform parent = myTransform.parent;
			int childCount = myTransform.childCount;

			for (int i = childCount - 1; i >= 0; i--) {
				myTransform.GetChild(i).SetParent(parent);
			}
		}

		#endregion
#endif
	}
}
