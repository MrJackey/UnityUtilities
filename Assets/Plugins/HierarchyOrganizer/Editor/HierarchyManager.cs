using System.Collections.Generic;
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
			ObjectFactory.componentWasAdded += OnComponentAdded;
			ObjectChangeEvents.changesPublished += OnObjectChangesPublished;

			Selection.selectionChanged += OnSelectionChanged;

			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnComponentAdded(Component component) {
			if (s_folders.Contains(component.gameObject.GetInstanceID())) {
				bool dialogResult = EditorUtility.DisplayDialog(
					"Hierarchy Organizer",
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

						if (IsFolder(destroyData.instanceId)) {
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
			if (Selection.count != 1) return false;

			GameObject selectedObject = Selection.activeGameObject;

			// Make sure there is a selection
			if (!selectedObject) return false;

			// Make sure selected object isn't an asset
			if (AssetDatabase.Contains(selectedObject))
				return false;

			// Prevent creating folder as child of non-folder
			if (selectedObject.transform.parent && !s_folders.Contains(selectedObject.transform.parent.gameObject.GetInstanceID()))
				return false;

			return !Application.isPlaying &&
			       PrefabStageUtility.GetCurrentPrefabStage() == null;
		}

		[MenuItem("GameObject/Create Folder Parent", priority = 0)]
		private static void MenuCreateFolderParent() {
			Undo.SetCurrentGroupName("Create Folder Parent");
			int undoIndex = Undo.GetCurrentGroup();

			GameObject folderObject = CreateFolder();

			Transform selectedTransform = Selection.activeTransform;
			int siblingIndex = selectedTransform.GetSiblingIndex();

			Undo.SetTransformParent(folderObject.transform, selectedTransform.parent, "");
			Undo.SetTransformParent(selectedTransform, folderObject.transform, "");

			folderObject.transform.SetSiblingIndex(siblingIndex);

			Undo.CollapseUndoOperations(undoIndex);

			HierarchyFolder folder = folderObject.GetComponent<HierarchyFolder>();
			folder.Initialize();

			HierarchyUtilities.SetExpanded(folderObject.GetInstanceID(), true);
			BeginObjectRename(folderObject);

			Tools.hidden = true;
		}

		private static GameObject CreateFolder() {
			GameObject go = new GameObject("Folder", typeof(HierarchyFolder));
			Undo.RegisterCreatedObjectUndo(go, "");

			return go;
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

		public static bool IsFolder(int instanceID) {
			return s_folders.Contains(instanceID);
		}
	}
}
