using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jackey.HierarchyOrganizer.Editor {
	internal static class HierarchyDrawer {
		private static Texture2D s_folderIcon;
		private static Texture2D s_folderOnIcon;
		private static Texture2D s_folderEmptyIcon;
		private static Texture2D s_folderEmptyOnIcon;
		private static Texture2D s_folderOpenedIcon;
		private static Texture2D s_folderOpenedOnIcon;

		static HierarchyDrawer() {
			CacheIcons();

			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
		}

		internal static void Init() { }

		private static void CacheIcons() {
			s_folderIcon = (Texture2D)EditorGUIUtility.IconContent("Folder Icon").image;
			s_folderOnIcon = (Texture2D)EditorGUIUtility.IconContent("Folder On Icon").image;
			s_folderEmptyIcon = (Texture2D)EditorGUIUtility.IconContent("FolderEmpty Icon").image;
			s_folderEmptyOnIcon = (Texture2D)EditorGUIUtility.IconContent("FolderEmpty On Icon").image;
			s_folderOpenedIcon = (Texture2D)EditorGUIUtility.IconContent("FolderOpened Icon").image;
			s_folderOpenedOnIcon = (Texture2D)EditorGUIUtility.IconContent("FolderOpened On Icon").image;
		}

		private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect) {
			if (selectionRect.y == 0) {
				// Setting an icon on the invisible RootOfAll GameObject stops the top-most folder icon from flickering.
				// Unfortunately it only seems to work when the hierarchy is not scrolled as the GameObject receiving the icon
				// seems to be required to be GUI-rendered. Do not know why this works but it does
				HierarchyUtilities.SetObjectIcon(0, null, null);
				return;
			}

			if (PrefabStageUtility.GetCurrentPrefabStage() == null && HierarchyManager.IsFolder(instanceID)) {
				GameObject folderGameObject = (GameObject)EditorUtility.InstanceIDToObject(instanceID);

				if (!folderGameObject)
					return;

				(Texture2D Normal, Texture2D Expanded) folderIcons = GetFolderIcon(folderGameObject);
				HierarchyUtilities.SetObjectIcon(instanceID, folderIcons.Normal, folderIcons.Expanded);
			}

			if (Settings.instance.IndentGuide != IndentGuide.None)
				DrawIndentGuide(instanceID, selectionRect);
		}

		private static (Texture2D Normal, Texture2D Expanded) GetFolderIcon(GameObject folder) {
			Texture2D normalIcon = null;
			Texture2D expandedIcon = null;

			if (folder.activeInHierarchy) {
				if (folder.transform.childCount == 0) {
					normalIcon = s_folderEmptyOnIcon;
				}
				else {
					normalIcon = s_folderOnIcon;
					expandedIcon = s_folderOpenedOnIcon;
				}
			}
			else {
				if (folder.transform.childCount == 0) {
					normalIcon = s_folderEmptyIcon;
				}
				else {
					normalIcon = s_folderIcon;
					expandedIcon = s_folderOpenedIcon;
				}
			}

			return (
				normalIcon,
				expandedIcon != null ? expandedIcon : normalIcon
			);
		}

		private static void DrawIndentGuide(int instanceID, Rect selectionRect) {
			const float ROOT_INDENT_SIZE = 46f;
			const float INDENT_SIZE = 14f;
			const float FOLDOUT_X_OFFSET = 8f;

			GameObject gameObject = (GameObject)EditorUtility.InstanceIDToObject(instanceID);

			// The GameObject is null when it is destroyed.
			// Unsure why that is the case and if there is a better way to handle this
			if (!gameObject) return;

			Rect verticalRect = selectionRect;
			verticalRect.xMin -= FOLDOUT_X_OFFSET;

			Color indentGuideColor = Settings.instance.IndentGuideColor;

			if (gameObject.transform.childCount == 0) {
				Rect horizontalRect = selectionRect;
				horizontalRect.xMin -= FOLDOUT_X_OFFSET;
				horizontalRect.width = 6f;

				horizontalRect.yMin += selectionRect.size.y / 2f;
				horizontalRect.height = 1f;

				EditorGUI.DrawRect(horizontalRect, indentGuideColor);

				verticalRect.width = 1f;
				EditorGUI.DrawRect(verticalRect, indentGuideColor);
			}

			float depth = (selectionRect.xMin - ROOT_INDENT_SIZE) / INDENT_SIZE;

			for (int i = 0; i < depth; i++) {
				verticalRect.xMin -= INDENT_SIZE;
				verticalRect.width = 1f;

				switch (Settings.instance.IndentGuide) {
					case IndentGuide.Lines:
						EditorGUI.DrawRect(verticalRect, indentGuideColor);

						break;
					case IndentGuide.Dots:
						DrawDotsIndentGuide(verticalRect, indentGuideColor);

						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private static void DrawDotsIndentGuide(Rect verticalRect, Color color) {
			const int INDENT_DOT_COUNT = 4;

			Rect dotRect = verticalRect;
			dotRect.height = 1f;
			dotRect.y--;

			int j = 0;
			do {
				dotRect.y += verticalRect.height / INDENT_DOT_COUNT;
				EditorGUI.DrawRect(dotRect, color);

				j++;
			} while (j < INDENT_DOT_COUNT);
		}

		internal enum IndentGuide {
			None,
			Lines,
			Dots,
		}
	}
}
