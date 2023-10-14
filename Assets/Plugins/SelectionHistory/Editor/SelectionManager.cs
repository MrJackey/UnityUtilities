using System;
using Jackey.SelectionHistory.Utilities;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.SelectionHistory.Editor {
	[InitializeOnLoad]
	public static class SelectionManager {
		private const string ALLOWED_SELECTIONS_PREF_KEY = "SelectionHistory-AllowedSelections";
		private const string SESSION_HISTORY_KEY = "SelectionHistory-SessionHistory";
		private const string SESSION_HISTORY_INDEX_KEY = "SelectionHistory-HistoryIndex";

		private static readonly RingBuffer<Object> s_history = new(10);
		private static int s_historyIndex = -1;

		private static SelectionTypes s_allowedSelections;

		internal static RingBuffer<Object> History => s_history;
		internal static int HistoryIndex => s_historyIndex;

		internal static SelectionTypes AllowedSelections {
			get => s_allowedSelections;
			set {
				if (s_allowedSelections == value) return;

				s_allowedSelections = value;
				EditorPrefs.SetInt(ALLOWED_SELECTIONS_PREF_KEY, (int)s_allowedSelections);
			}
		}

		internal static event Action<int> MovedInHistory;
		internal static event Action HistoryChanged;

		static SelectionManager() {
			AllowedSelections = (SelectionTypes)EditorPrefs.GetInt(ALLOWED_SELECTIONS_PREF_KEY, int.MaxValue);

			Selection.selectionChanged += OnSelectionChange;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
		}

		[MenuItem("Tools/Jackey/Selection History/Go Back", true, 1000)]
		internal static bool ValidateGoBack() {
			return s_historyIndex >= 0;
		}

		[MenuItem("Tools/Jackey/Selection History/Go Back", false, 1000)]
		[Shortcut("Selection History/Go Back")]
		internal static void GoBack() {
			while (s_historyIndex > 0) {
				s_historyIndex--;
				Object @object = s_history[s_historyIndex];

				if (@object != null) {
					EditorGUIUtility.PingObject(@object);
					MovedInHistory?.Invoke(s_historyIndex);
					break;
				}

				if (s_historyIndex == 0) {
					MovedInHistory?.Invoke(s_historyIndex);
					break;
				}
			}
		}

		[MenuItem("Tools/Jackey/Selection History/Go Forward", true, 1001)]
		internal static bool ValidateGoForward() {
			return s_history.Count > 0 && s_historyIndex <= s_history.Count - 1;
		}

		[MenuItem("Tools/Jackey/Selection History/Go Forward", false, 1001)]
		[Shortcut("Selection History/Go Forward")]
		internal static void GoForward() {
			if (s_history.Count == 0)
				return;

			while (s_historyIndex < s_history.Count - 1) {
				s_historyIndex++;
				Object @object = s_history[s_historyIndex];

				if (@object != null) {
					EditorGUIUtility.PingObject(@object);
					MovedInHistory?.Invoke(s_historyIndex);
					break;
				}

				if (s_historyIndex == s_history.Count - 1) {
					MovedInHistory?.Invoke(s_historyIndex);
					break;
				}
			}
		}

		[MenuItem("Tools/Jackey/Selection History/Clear Selection History", true, 1014)]
		internal static bool ValidateClearHistory() {
			return s_history.Count > 0;
		}

		[MenuItem("Tools/Jackey/Selection History/Clear Selection History", false, 1014)]
		internal static void ClearHistory() {
			s_history.Clear();
			s_historyIndex = -1;

			HistoryChanged?.Invoke();
		}

		private static void OnSelectionChange() {
			if (AllowedSelections == 0)
				return;

			Object selectedObject = Selection.activeObject;

			// Deselect
			if (selectedObject == null)
				return;

			// Ignore Animator objects
			if (IsPartOfAnimator(selectedObject))
				return;

			// Ignore selected packages in Package Manager Window
			if (selectedObject.GetType().FullName == "UnityEditor.PackageManager.UI.Internal.PackageSelectionObject")
				return;

			bool selectedAsset = AssetDatabase.Contains(selectedObject);

			// Selected Asset
			if ((s_allowedSelections & SelectionTypes.Assets) == 0 && selectedAsset)
				return;

			// Selected Non-Asset
			if ((s_allowedSelections & SelectionTypes.SceneObjects) == 0 && !selectedAsset)
				return;

			// Don't add the current selection twice
			if (s_history.Count > 0 && s_history[s_historyIndex] == selectedObject)
				return;

			// Remove any future history when branching
			for (int i = s_history.Count - 1; i > s_historyIndex; i--)
				s_history.RemoveAt(i);

			s_history.Add(selectedObject);
			s_historyIndex = s_history.Count - 1;

			HistoryChanged?.Invoke();
		}

		private static bool IsPartOfAnimator(Object @object) {
			if (@object is
			    AnimatorStateMachine or
			    AnimatorState or
			    BlendTree or
			    AnimatorStateTransition) {
				return true;
			}

			string fullTypeName = @object.GetType().FullName;

			return fullTypeName is "UnityEditor.Animations.AnimatorDefaultTransition" or
				"UnityEditor.Graphs.AnimationStateMachine.EntryNode" or
				"UnityEditor.Graphs.AnimationStateMachine.ExitNode" or
				"UnityEditor.Graphs.AnimationStateMachine.AnyStateNode";
		}

		internal static void MoveToIndex(int index) {
			s_historyIndex = index;

			Object @object = s_history[s_historyIndex];

			if (@object != null) {
				EditorGUIUtility.PingObject(@object);
			}

			MovedInHistory?.Invoke(s_historyIndex);
		}

		#region Assembly Reload Persistance

		private static void OnBeforeAssemblyReload() {
			int[] historyIDs = new int[s_history.Count];

			for (int i = 0; i < s_history.Count; i++) {
				Object @object = s_history[i];
				historyIDs[i] = (@object ? @object.GetInstanceID() : default);
			}

			if (historyIDs.Length > 0) {
				SessionState.SetIntArray(SESSION_HISTORY_KEY, historyIDs);
				SessionState.SetInt(SESSION_HISTORY_INDEX_KEY, s_historyIndex);
			}
		}

		private static void OnAfterAssemblyReload() {
			int[] historyIDs = SessionState.GetIntArray(SESSION_HISTORY_KEY, Array.Empty<int>());
			SessionState.EraseIntArray(SESSION_HISTORY_KEY);

			foreach (int id in historyIDs) {
				s_history.Add(EditorUtility.InstanceIDToObject(id));
			}

			if (historyIDs.Length > 0) {
				s_historyIndex = SessionState.GetInt(SESSION_HISTORY_INDEX_KEY, -1);
				SessionState.EraseInt(SESSION_HISTORY_INDEX_KEY);
			}
		}

		#endregion
	}
}
