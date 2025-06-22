using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.HierarchyOrganizer.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.HierarchyOrganizer.Editor {
	[InitializeOnLoad]
	internal static class HierarchyManager {
		private static readonly HashSet<int> s_folders = new();

		static HierarchyManager() {
			HierarchyUtilities.Init();
			HierarchyDrawer.Init();

			HierarchyFolder.Initialized += RegisterFolder;
			HierarchyFolder.Destroyed += UnregisterFolder;
			ObjectFactory.componentWasAdded += OnComponentAdded;
			ObjectChangeEvents.changesPublished += OnObjectChangesPublished;

			Selection.selectionChanged += OnSelectionChanged;

			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnComponentAdded(Component component) {
			if (s_folders.Contains(component.gameObject.GetInstanceID())) {
				bool dialogResult = EditorUtility.DisplayDialog(
					PluginInfo.NAME,
					$"Component ({component.GetType().Name}) was just added to a folder.\n\nA folder should not have any components as all components on a folder will be lost when they are stripped from the scene",
					"Destroy the component",
					"Keep the component"
				);

				if (dialogResult)
					EditorApplication.delayCall += () => Object.DestroyImmediate(component);
			}
		}

		private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream) {
			for (int i = 0; i < stream.length; i++) {
				ObjectChangeKind changeType = stream.GetEventType(i);

				switch (changeType) {
					// Listening for GameObject destruction here as for some reason, OnDestroy on the folders
					// is not called when redoing a deletion. This is the only (and most?) consistent way I have
					// found to be notified of GameObject destruction. However it only invokes once for the root GameObject
					// hence the use of RemoveWhere to catch any nested folders
					case ObjectChangeKind.DestroyGameObjectHierarchy:
						stream.GetDestroyGameObjectHierarchyEvent(i, out DestroyGameObjectHierarchyEventArgs destroyData);

						if (IsInitializedFolder(destroyData.instanceId)) {
							s_folders.RemoveWhere(instanceId => EditorUtility.InstanceIDToObject(instanceId) == null);
						}

						// Destroying game objects seems to change which tree view item is used to render the existing objects whilst
						// keeping the old connection. This makes the cache outdated and it must therefore be refreshed.
						EditorApplication.delayCall += () => {
							HierarchyUtilities.ClearCache();
							HierarchyUtilities.Repaint();
						};

						break;
				}
			}
		}

		private static void OnSelectionChanged() {
			// Prevent using tools while a folder is selected
			foreach (int instanceID in Selection.instanceIDs) {
				if (s_folders.Contains(instanceID)) {
					Tools.hidden = true;
					return;
				}
			}

			Tools.hidden = false;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
			// For some reason, making script changes while in playmode causes the folders collection
			// to not be cleared when entering playmode the next time. I'm therefore clearing it manually
			if (stateChange == PlayModeStateChange.EnteredPlayMode) {
				s_folders.Clear();
			}
		}

		#region Folder Creation

		[MenuItem("GameObject/Create Folder", true)]
		private static bool ValidateMenuCreateFolder() {
			if (Selection.count > 1) return false;

			GameObject selectedObject = Selection.activeGameObject;
			if (selectedObject) {
				// Make sure selected object isn't an asset
				if (AssetDatabase.Contains(selectedObject))
					return false;

				// Prevent creating folder as child of non-folder
				Transform selectedObjParent = selectedObject.transform;
				if (selectedObjParent && !s_folders.Contains(selectedObjParent.gameObject.GetInstanceID()))
					return false;
			}

			return !Application.isPlaying &&
			       PrefabStageUtility.GetCurrentPrefabStage() == null;
		}

		[MenuItem("GameObject/Create Folder", priority = 0)]
		private static void MenuCreateFolder() {
			Undo.SetCurrentGroupName("Create Folder");
			int undoIndex = Undo.GetCurrentGroup();

			GameObject folderObject = CreateFolder();

			Undo.SetTransformParent(folderObject.transform, Selection.activeTransform, "");

			Undo.CollapseUndoOperations(undoIndex);

			HierarchyFolder folder = folderObject.GetComponent<HierarchyFolder>();
			folder.Initialize();

			BeginObjectRename(folderObject);
			Tools.hidden = true;
		}

		[MenuItem("GameObject/Create Folder Parent", true)]
		private static bool ValidateMenuCreateFolderParent() {
			if (Application.isPlaying)
				return false;

			if (PrefabStageUtility.GetCurrentPrefabStage() != null)
				return false;

			GameObject[] selectedGameObjects = Selection.gameObjects;
			if (selectedGameObjects.Length == 0)
				return false;

			Transform parent = selectedGameObjects[0].transform.parent;

			// Prevent creating folder as child of non-folder
			if (parent != null && !s_folders.Contains(parent.gameObject.GetInstanceID()))
				return false;

			foreach (GameObject selectedObject in selectedGameObjects) {
				// Make sure the selected is valid
				if (selectedObject == null)
					return false;

				// Make sure selected object isn't an asset
				if (AssetDatabase.Contains(selectedObject))
					return false;

				// Make sure all selected objects share the same parent
				if (selectedObject.transform.parent != parent)
					return false;
			}

			return true;
		}

		[MenuItem("GameObject/Create Folder Parent", priority = 0)]
		private static void MenuCreateFolderParent(MenuCommand cmd) {
			// Make sure this menu item is only executed once with multiple selected objects
			if (cmd.context != null && cmd.context != Selection.activeObject)
				return;

			Undo.SetCurrentGroupName("Create Folder Parent");
			int undoIndex = Undo.GetCurrentGroup();

			GameObject folderObject = CreateFolder();

			Transform[] selectedTransforms = Selection.transforms;
			Undo.SetTransformParent(folderObject.transform, selectedTransforms[0].parent, "");

			Array.Sort(selectedTransforms, (lhs, rhs) => lhs.GetSiblingIndex() - rhs.GetSiblingIndex());
			int siblingIndex = selectedTransforms[0].GetSiblingIndex();

			foreach (Transform selectedTransform in selectedTransforms) {
				Undo.SetTransformParent(selectedTransform, folderObject.transform, "");
			}

			folderObject.transform.SetSiblingIndex(siblingIndex);

			Undo.CollapseUndoOperations(undoIndex);

			HierarchyFolder folder = folderObject.GetComponent<HierarchyFolder>();
			folder.Initialize();

			HierarchyUtilities.SetExpanded(folderObject.GetInstanceID(), true);
			BeginObjectRename(folderObject);

			Tools.hidden = true;
		}

		[MenuItem("GameObject/Convert to Folder(s)", true)]
		private static bool ValidateMenuConvertToFolder() {
			if (PrefabStageUtility.GetCurrentPrefabStage() != null)
				return false;

			foreach (GameObject selectedGo in Selection.gameObjects) {
				if (CanGameObjectBeFolder(selectedGo))
					return true;
			}

			return false;
		}

		[MenuItem("GameObject/Convert to Folder(s)", priority = 0)]
		private static void MenuConvertToFolder() {
			Undo.SetCurrentGroupName("Convert to Folder");
			int undoIndex = Undo.GetCurrentGroup();

			IOrderedEnumerable<GameObject> selectionDepthLast = Selection.gameObjects.OrderBy(go => {
				int depth = 0;

				Transform ancestor = go.transform.parent;
				while (ancestor) {
					depth++;
					ancestor = ancestor.parent;
				}

				return depth;
			});

			foreach (GameObject selectedGo in selectionDepthLast) {
				if (!CanGameObjectBeFolder(selectedGo))
					continue;

				Transform selectedTransform = selectedGo.transform;
				int childCount = selectedTransform.childCount;
				Matrix4x4[] childTransforms = new Matrix4x4[childCount];

				// Record child transforms
				for (int i = 0; i < childCount; i++) {
					Transform child = selectedTransform.GetChild(i);
					childTransforms[i] = Matrix4x4.TRS(child.position, child.rotation, child.lossyScale);
				}

				// Reset the folder's transform
				Undo.AddComponent<HierarchyFolder>(selectedGo);
				Undo.RecordObject(selectedTransform, "");
				selectedTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				selectedTransform.localScale = Vector3.one;

				// Apply the child transforms to match before conversion
				for (int i = 0; i < childCount; i++) {
					Transform child = selectedTransform.GetChild(i);
					Matrix4x4 worldTransform = childTransforms[i];

					Undo.RecordObject(child, "");
					child.SetPositionAndRotation(worldTransform.GetPosition(), worldTransform.rotation);
					child.localScale = worldTransform.lossyScale;
				}
			}

			Undo.CollapseUndoOperations(undoIndex);
		}

		[MenuItem("GameObject/Remove as Folder(s)", true)]
		private static bool ValidateMenuRemoveAsFolder() {
			foreach (GameObject selectedGo in Selection.gameObjects) {
				if (CanFolderBeRemoved(selectedGo))
					return true;
			}

			return false;
		}

		[MenuItem("GameObject/Remove as Folder(s)", priority = 0)]
		private static void MenuRemoveAsFolder() {
			foreach (GameObject selectedGo in Selection.gameObjects) {
				if (!CanFolderBeRemoved(selectedGo))
					continue;

				Undo.DestroyObjectImmediate(selectedGo.GetComponent<HierarchyFolder>());
			}
		}

		private static GameObject CreateFolder() {
			GameObject go = new GameObject("Folder", typeof(HierarchyFolder));
			Undo.RegisterCreatedObjectUndo(go, "");

			return go;
		}

		private static bool CanGameObjectBeFolder(GameObject go) {
			if (EditorUtility.IsPersistent(go))
				return false;

			if (PrefabUtility.IsPartOfAnyPrefab(go))
				return false;

			if (IsFolder(go.GetInstanceID()))
				return false;

			if (go.GetComponentCount() > 1)
				return false;

			Transform parent = go.transform.parent;
			if (parent == null)
				return true;

			GameObject parentGo = parent.gameObject;
			if (IsFolder(parentGo.GetInstanceID()) || (Selection.gameObjects.Contains(parentGo) && CanGameObjectBeFolder(parentGo)))
				return true;

			return false;
		}

		private static bool CanFolderBeRemoved(GameObject go) {
			if (!IsFolder(go.GetInstanceID()))
				return false;

			Transform transform = go.transform;
			int childCount = transform.childCount;

			for (int i = 0; i < childCount; i++) {
				Transform child = transform.GetChild(i);

				if (!child.TryGetComponent(out HierarchyFolder _))
					return true;

				GameObject childGo = child.gameObject;
				if (!Selection.gameObjects.Contains(childGo) || !CanFolderBeRemoved(childGo))
					return false;
			}

			return true;
		}

		private static void BeginObjectRename(GameObject go) {
			Selection.activeGameObject = go;

			EditorApplication.ExecuteMenuItem("Edit/Rename");
		}

		#endregion

		[DidReloadScripts]
		private static void OnScriptReload() {
			RegisterExistingFolders();
		}

		private static void RegisterExistingFolders() {
			foreach (HierarchyFolder folder in Object.FindObjectsOfType<HierarchyFolder>(true)) {
				RegisterFolder(folder);
			}
		}

		private static void RegisterFolder(HierarchyFolder folder) {
			int folderID = folder.gameObject.GetInstanceID();

			s_folders.Add(folderID);
		}

		private static void UnregisterFolder(HierarchyFolder folder) {
			int folderID = folder.gameObject.GetInstanceID();

			s_folders.Remove(folderID);
		}

		public static bool IsFolder(int instanceID) {
			return s_folders.Contains(instanceID);
		}
	}
}
