using System;
using System.Linq;
using Jackey.HierarchyOrganizer.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Jackey.HierarchyOrganizer.Editor {
	internal class HierarchyFolderProcessor : IProcessSceneWithReport, IPreprocessBuildWithReport {
		private static bool s_removeAsException;

		public int callbackOrder => -1000;

		public void OnPreprocessBuild(BuildReport _) {
			s_removeAsException = false;

			Settings settings = Settings.instance;
			if (!settings.StripOnBuilds && settings.WarnOfDisabledStripBuilds) {
				s_removeAsException = EditorUtility.DisplayDialog(
					PluginInfo.NAME,
					"A build has started with folder stripping disabled. Folders should almost always be removed from builds. Do you want to remove the folders in this build as an exception?\n\nThis warning can be suppressed in the settings.",
					"Remove",
					"Keep"
				);
			}
		}

		public void OnProcessScene(Scene _, BuildReport __) {
			if (BuildPipeline.isBuildingPlayer) {
				HandlePlayerBuild();
			}
			else {
				HandleEditorPlaymodeEnter();
			}
		}

		private static void HandlePlayerBuild() {
			if (Settings.instance.StripOnBuilds || s_removeAsException) {
				StripFolders();
			}
		}

		private static void HandleEditorPlaymodeEnter() {
			if (Settings.instance.StripInEditor) {
				StripFolders();
			}
		}

		private static void StripFolders() {
			foreach (HierarchyFolder folder in Object.FindObjectsOfType<HierarchyFolder>(true).OrderByDescending(x => x.GetDepth())) {
				GameObject folderObject = folder.gameObject;

				if (folderObject.GetComponentCount() > 2) // Expects Transform & HierarchyFolder
					Debug.LogWarning($"Folder \"{folder.name}\" in scene {folderObject.scene.name} has components on it which is lost during folder stripping", folder);

				if (folderObject.activeInHierarchy) {
					folder.Flatten();
				}
				else {
					switch (Settings.instance.DisabledFolderStripMethod) {
						case DisabledFolderStripMethod.DestroyChildren:
							folder.DestroyChildren();
							break;
						case DisabledFolderStripMethod.DisableChildren:
							folder.FlattenAndDisableChildren();
							break;
						case DisabledFolderStripMethod.DoNothing:
							folder.Flatten();
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (Application.isPlaying)
					Object.Destroy(folderObject);
				else
					Object.DestroyImmediate(folderObject);
			}
		}

		public enum DisabledFolderStripMethod {
			DestroyChildren,
			DisableChildren,
			DoNothing,
		}
	}
}
