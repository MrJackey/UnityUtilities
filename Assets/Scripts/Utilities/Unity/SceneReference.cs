using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Jackey.Utilities.Unity {
	/// <summary>
	/// Save a reference to a scene asset
	/// </summary>
	[Serializable]
	public struct SceneReference : ISerializationCallbackReceiver, IEquatable<SceneReference> {
#if UNITY_EDITOR
		[SerializeField] private string m_sceneGuid;
#endif
		[SerializeField] private int m_buildIndex;

		/// <summary>
		/// The build index of the referenced scene
		/// </summary>
		public int BuildIndex {
			get {
#if UNITY_EDITOR
				return GetBuildIndex();
#else
				return m_buildIndex;
#endif
			}
		}

		/// <summary>
		/// Is the reference valid?
		/// </summary>
		public bool IsValid {
			get {
#if UNITY_EDITOR
				return GetBuildIndex() != -1;
#else
				return m_buildIndex != -1;
#endif
			}
		}

		public SceneReference(int buildIndex) : this() {
			m_buildIndex = buildIndex;
		}

		public SceneReference(Scene scene) : this() {
			m_buildIndex = scene.buildIndex;
		}

		#region Serialization

		public void OnBeforeSerialize() {
#if UNITY_EDITOR
			m_buildIndex = GetBuildIndex();
#endif
		}

		public void OnAfterDeserialize() { }

		#endregion

		/// <summary>
		/// Load the referenced scene
		/// </summary>
		public void Load() {
			if (!IsValid) {
				Debug.LogWarning("Unable to load scene. Reference is invalid");
				return;
			}

#if UNITY_EDITOR
			SceneManager.LoadScene(GetBuildIndex());
#else
			SceneManager.LoadScene(m_buildIndex);
#endif
		}

		/// <summary>
		/// Load the referenced scene using the specified load mode
		/// </summary>
		public void Load(LoadSceneMode loadMode) {
			if (!IsValid) {
				Debug.LogWarning("Unable to load scene. Reference is invalid");
				return;
			}

#if UNITY_EDITOR
			SceneManager.LoadScene(GetBuildIndex(), loadMode);
#else
			SceneManager.LoadScene(m_buildIndex, loadMode);
#endif
		}

		/// <summary>
		/// Begin asynchronous loading of the referenced scene
		/// </summary>
		/// <returns>Returns the scene's load operation</returns>
		public AsyncOperation LoadAsync() {
			if (!IsValid) {
				Debug.LogWarning("Unable to load scene. Reference is invalid");
				return null;
			}

#if UNITY_EDITOR
			return SceneManager.LoadSceneAsync(GetBuildIndex());
#else
			return SceneManager.LoadSceneAsync(m_buildIndex);
#endif
		}

		/// <summary>
		/// Begin asynchronous loading of the referenced scene using the specified load mode
		/// </summary>
		/// <returns>Returns the scene's load operation</returns>
		public AsyncOperation LoadAsync(LoadSceneMode loadMode) {
			if (!IsValid) {
				Debug.LogWarning("Unable to load scene. Reference is invalid");
				return null;
			}

#if UNITY_EDITOR
			return SceneManager.LoadSceneAsync(GetBuildIndex(), loadMode);
#else
			return SceneManager.LoadSceneAsync(m_buildIndex, loadMode);
#endif
		}

#if UNITY_EDITOR
		private int GetBuildIndex() {
			if (string.IsNullOrEmpty(m_sceneGuid))
				return -1;

			string sceneAssetPath = AssetDatabase.GUIDToAssetPath(m_sceneGuid);
			return SceneUtility.GetBuildIndexByScenePath(sceneAssetPath);
		}
#endif

		public override string ToString() {
#if UNITY_EDITOR
			return GetBuildIndex().ToString();
#else
			return m_buildIndex.ToString();
#endif
		}

		public static implicit operator int(SceneReference reference) {
#if UNITY_EDITOR
			return reference.GetBuildIndex();
#else
			return reference.m_buildIndex;
#endif
		}

		public static bool operator ==(SceneReference lhs, SceneReference rhs) => lhs.Equals(rhs);
		public static bool operator !=(SceneReference lhs, SceneReference rhs) => !lhs.Equals(rhs);

		public override bool Equals(object obj) => obj is SceneReference other && Equals(other);
		public bool Equals(SceneReference other) {
#if UNITY_EDITOR
			return m_sceneGuid == other.m_sceneGuid;
#else
			return m_buildIndex == other.m_buildIndex;
#endif
		}

		public override int GetHashCode() {
#if UNITY_EDITOR
			return m_sceneGuid.GetHashCode();
#else
			return m_buildIndex;
#endif
		}
	}

	namespace PropertyDrawers {
#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(SceneReference))]
		public class SceneReferencePropertyDrawer : PropertyDrawer {
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				Color defaultGUIBackgroundColor = GUI.backgroundColor;

				label = EditorGUI.BeginProperty(position, label, property);

				SerializedProperty guidProperty = property.FindPropertyRelative("m_sceneGuid");
				string sceneGuid = guidProperty.stringValue;

				string scenePath = null;
				SceneAsset sceneAsset = null;
				EditorBuildSettingsScene buildScene = null;

				if (!string.IsNullOrEmpty(sceneGuid)) {
					scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

					if (!string.IsNullOrEmpty(scenePath)) {
						sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

						buildScene = EditorBuildSettings.scenes.FirstOrDefault(x =>
							!string.IsNullOrEmpty(x.path) && x.guid.ToString() == sceneGuid
						);

						if (buildScene == null) {
							GUI.backgroundColor = Color.red;
							label.tooltip = "Scene is not present in build settings";
						}
						else if (!buildScene.enabled) {
							GUI.backgroundColor = Color.yellow;
							label.tooltip = "Scene is disabled in build settings";
						}
					}
				}

				position = EditorGUI.PrefixLabel(position, label);

				if (sceneAsset != null && CheckContextMenu(position)) {
					CreateContextMenu(scenePath, sceneAsset, buildScene);
					Event.current.Use();
				}

				EditorGUI.BeginChangeCheck();
				Object fieldValue = EditorGUI.ObjectField(position, GUIContent.none, sceneAsset, typeof(SceneAsset), false);
				if (EditorGUI.EndChangeCheck()) {
					string assetPath = AssetDatabase.GetAssetPath(fieldValue);
					guidProperty.stringValue = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
				}

				EditorGUI.EndProperty();

				GUI.backgroundColor = defaultGUIBackgroundColor;
			}

			private bool CheckContextMenu(Rect position) {
				Event evt = Event.current;
				return evt.type == EventType.MouseDown && evt.button == 1 && position.Contains(evt.mousePosition);
			}

			private void CreateContextMenu(string scenePath, SceneAsset sceneAsset, EditorBuildSettingsScene buildScene) {
				GenericMenu menu = new GenericMenu();

				if (!EditorApplication.isPlayingOrWillChangePlaymode) {
					menu.AddItem(new GUIContent("Open Scene"), false, () => {
						if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
							EditorSceneManager.OpenScene(scenePath);
					});
					menu.AddItem(new GUIContent("Add Scene"), false, () => EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive));
					menu.AddSeparator("");
				}

				if (buildScene != null) {
					menu.AddItem(new GUIContent("Remove from Build Settings"), false, () => RemoveFromBuildSettings(buildScene));
					menu.AddItem(new GUIContent("Enabled in Build Settings"), buildScene.enabled, () => ToggleBuildEnabled(buildScene));
				}
				else {
					menu.AddItem(new GUIContent("Add to Build Settings"), false, () => AddToBuildSettings(scenePath));
				}

				menu.AddSeparator("");

				menu.AddItem(new GUIContent("Properties..."), false, () => EditorUtility.OpenPropertyEditor(sceneAsset));

				menu.ShowAsContext();
			}

			private static void AddToBuildSettings(string scenePath) {
				EditorBuildSettingsScene buildScene = new EditorBuildSettingsScene(scenePath, true);
				EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(buildScene).ToArray();
			}

			private static void RemoveFromBuildSettings(EditorBuildSettingsScene buildScene) {
				List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
				int buildSceneIndex = buildScenes.FindIndex(x => x.guid == buildScene.guid);
				buildScenes.RemoveAt(buildSceneIndex);
				EditorBuildSettings.scenes = buildScenes.ToArray();
			}

			private static void ToggleBuildEnabled(EditorBuildSettingsScene buildScene) {
				EditorBuildSettingsScene[] editorBuildScenes = EditorBuildSettings.scenes;
				EditorBuildSettingsScene editorBuildScene = Array.Find(editorBuildScenes, x => x.guid == buildScene.guid);
				editorBuildScene.enabled = !editorBuildScene.enabled;
				EditorBuildSettings.scenes = editorBuildScenes;
			}
		}
#endif
	}
}
