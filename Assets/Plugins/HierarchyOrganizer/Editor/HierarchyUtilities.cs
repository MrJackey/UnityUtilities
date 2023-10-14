using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Jackey.HierarchyOrganizer.Editor {
	public static class HierarchyUtilities {
		#region Reflection Fields

		private static readonly Func<IList> s_getAllHierarchyWindowsMethod; // Func<List<SceneHierarchyWindow>>

		private static readonly PropertyInfo s_sceneHierarchyProperty; // SceneHierarchy
		private static readonly MethodInfo s_setObjectExpandedMethod; // Action<int, bool>

		private static readonly PropertyInfo s_treeViewControllerProperty; // TreeViewController
		private static readonly MethodInfo s_findTreeViewItemMethod; // Func<int, TreeViewItem>

		private static readonly PropertyInfo s_treeViewExpandedStateChangedProperty; // Action { get; set; }

		private static readonly PropertyInfo s_treeViewDataProperty; // ITreeViewDataSource
		private static readonly MethodInfo s_isExpandedMethod; // Func<int, bool>

		#endregion

		private static readonly object[] s_singleObjectArray = new object[1];

		private static float s_lastRectY;
		private static bool s_isDirty = true;

		private static IList s_hierarchyWindows;
		private static ArrayList s_sceneHierarchies = new();
		private static ArrayList s_treeViews = new();
		private static ArrayList s_treeViewsData = new();

		private static readonly Dictionary<object, Dictionary<int, TreeViewItem>> s_treeViewItemCache = new();
		private static List<object> s_treeViewCacheToRemove = new();

		static HierarchyUtilities() {
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;

			#region Reflection Caching

			Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
			Type hierarchyWindowType = editorAssembly.GetType("UnityEditor.SceneHierarchyWindow");
			s_getAllHierarchyWindowsMethod = hierarchyWindowType
				.GetMethod("GetAllSceneHierarchyWindows", BindingFlags.Static | BindingFlags.Public)
				.CreateDelegate(typeof(Func<IList>)) as Func<IList>;
			s_sceneHierarchyProperty = hierarchyWindowType
				.GetProperty("sceneHierarchy");

			Type sceneHierarchyType = editorAssembly.GetType("UnityEditor.SceneHierarchy");
			s_treeViewControllerProperty = sceneHierarchyType
				.GetProperty("treeView", BindingFlags.Instance | BindingFlags.NonPublic);
			s_setObjectExpandedMethod = sceneHierarchyType
				.GetMethod("ExpandTreeViewItem", BindingFlags.NonPublic | BindingFlags.Instance);

			Type treeViewControllerType = editorAssembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
			s_findTreeViewItemMethod = treeViewControllerType
				.GetMethod("FindItem");
			s_treeViewDataProperty = treeViewControllerType
				.GetProperty("data", BindingFlags.Instance | BindingFlags.Public);

			s_treeViewExpandedStateChangedProperty = treeViewControllerType.
				GetProperty("expandedStateChanged", BindingFlags.Instance | BindingFlags.Public);

			Type treeViewDataType = editorAssembly.GetType("UnityEditor.IMGUI.Controls.ITreeViewDataSource");
			s_isExpandedMethod = treeViewDataType
				.GetMethod("IsExpanded", new[] { typeof(TreeViewItem) });

			#endregion

			RefreshReflection();
		}

		internal static void Init() { }

		public static void SetObjectIcon(int instanceID, Texture2D normalIcon, Texture2D expandedIcon) {
			CheckReflection();

			for (int i = 0; i < s_treeViews.Count; i++) {
				TreeViewItem treeViewItem = FindTreeViewItem(s_treeViews[i], instanceID);

				// Creating a folder when using multiple hierarchy windows delays the tree view item creation for one frame resulting in null
				if (treeViewItem == null)
					continue;

				s_singleObjectArray[0] = treeViewItem;
				bool isExpanded = (bool)s_isExpandedMethod.Invoke(s_treeViewsData[i], s_singleObjectArray);
				treeViewItem.icon = isExpanded ? expandedIcon : normalIcon;
			}
		}

		public static void SetExpanded(int instanceID, bool expanded) {
			CheckReflection();

			foreach (object sceneHierarchy in s_sceneHierarchies) {
				s_setObjectExpandedMethod.Invoke(sceneHierarchy, new object[] { instanceID, expanded });
			}
		}

		public static void Repaint() {
			foreach (EditorWindow window in s_hierarchyWindows) {
				window.Repaint();
			}
		}

		private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect) {
			if (selectionRect.y < s_lastRectY)
				s_isDirty = true;

			s_lastRectY = selectionRect.y;
		}

		private static void CheckReflection() {
			if (s_isDirty)
				RefreshReflection();
		}

		private static void RefreshReflection() {
			// Can also be done using the Resources API: Resources.FindObjectsOfTypeAll(_);
			s_hierarchyWindows = s_getAllHierarchyWindowsMethod.Invoke();

			s_sceneHierarchies.Clear();
			foreach (object hierarchyWindow in s_hierarchyWindows) {
				s_sceneHierarchies.Add(s_sceneHierarchyProperty.GetValue(hierarchyWindow));
			}

			s_treeViews.Clear();
			foreach (object sceneHierarchy in s_sceneHierarchies) {
				object treeViewController = s_treeViewControllerProperty.GetValue(sceneHierarchy);
				s_treeViews.Add(treeViewController);

				// It seems that certain tree view items swap whenever an object expands/folds. The cached tree view item
				// does however persist which does not clear the cache using the id check. Hence the clear on any expand changes
				s_treeViewExpandedStateChangedProperty.SetValue(
					treeViewController,
					(Action)s_treeViewExpandedStateChangedProperty.GetValue(treeViewController) + ClearCache
				);
			}

			s_treeViewsData.Clear();
			foreach (object treeView in s_treeViews) {
				s_treeViewsData.Add(s_treeViewDataProperty.GetValue(treeView));
			}

			s_treeViewCacheToRemove.Clear();
			foreach (object treeView in s_treeViewItemCache.Keys) {
				if (!s_treeViews.Contains(treeView)) {
					s_treeViewCacheToRemove.Add(treeView);
				}
			}

			foreach (object treeView in s_treeViewCacheToRemove) {
				s_treeViewItemCache.Remove(treeView);
			}

			s_isDirty = false;
		}

		private static TreeViewItem FindTreeViewItem(object treeView, int instanceID) {
			if (s_treeViewItemCache.TryGetValue(treeView, out Dictionary<int, TreeViewItem> treeViewItems)) {
				if (treeViewItems.TryGetValue(instanceID, out TreeViewItem treeViewItem)) {
					// The TreeViewItems seem to be reused internally making this connection able to be wrong.
					// Best way to handle this I can think of is clearing the cache and redoing it.
					if (instanceID != treeViewItem.id) {
						treeViewItems.Clear();

						return FindTreeViewItem(treeView, instanceID);
					}

					return treeViewItem;
				}

				return CacheTreeViewItem(treeView, instanceID, treeViewItems);
			}

			Dictionary<int, TreeViewItem> newTreeViewItems = new Dictionary<int, TreeViewItem>();
			s_treeViewItemCache.Add(treeView, newTreeViewItems);

			return CacheTreeViewItem(treeView, instanceID, newTreeViewItems);
		}

		private static TreeViewItem CacheTreeViewItem(object treeView, int instanceID, Dictionary<int, TreeViewItem> cache) {
			s_singleObjectArray[0] = instanceID;
			TreeViewItem treeViewItem = (TreeViewItem)s_findTreeViewItemMethod.Invoke(treeView, s_singleObjectArray);

			if (treeViewItem != null)
				cache.Add(instanceID, treeViewItem);

			return treeViewItem;
		}

		internal static void ClearCache() {
			s_treeViewItemCache.Clear();
			s_isDirty = true;
		}
	}
}
