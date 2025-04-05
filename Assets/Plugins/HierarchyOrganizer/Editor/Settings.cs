using UnityEditor;
using UnityEngine;

namespace Jackey.HierarchyOrganizer.Editor {
	[FilePath("UserSettings/HierarchyOrganizerUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	internal class Settings : ScriptableSingleton<Settings> {
		private const string FOLDER_BUILD_STRIP_MENU_PATH = "Tools/Jackey/Hierarchy Organizer/Strip Folders on Builds";
		private const string FOLDER_EDITOR_STRIP_MENU_PATH = "Tools/Jackey/Hierarchy Organizer/Strip Folders in Editor Playmode";

		private const bool SAVE_AS_TEXT = true;

		[SerializeField] private bool m_stripOnBuilds = true;
		[SerializeField] private bool m_stripInEditor;
		[SerializeField] private HierarchyFolderProcessor.DisabledFolderStripMethod m_disabledFolderStripMethod = HierarchyFolderProcessor.DisabledFolderStripMethod.DestroyChildren;

		[SerializeField] private bool m_warnOfDisabledStripBuilds = true;

		[SerializeField] private HierarchyDrawer.IndentGuide m_indentGuide = HierarchyDrawer.IndentGuide.None;
		[SerializeField] private Color m_indentGuideColor = new(0.39f, 0.4f, 0.39f);

		internal bool StripOnBuilds {
			get => m_stripOnBuilds;
			set {
				m_stripOnBuilds = value;
				Menu.SetChecked(FOLDER_BUILD_STRIP_MENU_PATH, value);
			}
		}

		internal bool StripInEditor {
			get => m_stripInEditor;
			set {
				m_stripInEditor = value;
				Menu.SetChecked(FOLDER_EDITOR_STRIP_MENU_PATH, value);
			}
		}

		internal HierarchyFolderProcessor.DisabledFolderStripMethod DisabledFolderStripMethod {
			get => m_disabledFolderStripMethod;
			set => m_disabledFolderStripMethod = value;
		}

		internal bool WarnOfDisabledStripBuilds {
			get => m_warnOfDisabledStripBuilds;
			set => m_warnOfDisabledStripBuilds = value;
		}

		internal HierarchyDrawer.IndentGuide IndentGuide {
			get => m_indentGuide;
			set => m_indentGuide = value;
		}

		internal Color IndentGuideColor {
			get => m_indentGuideColor;
			set => m_indentGuideColor = value;
		}

		private void Init() {
			EditorApplication.delayCall += () => {
				Menu.SetChecked(FOLDER_BUILD_STRIP_MENU_PATH, instance.StripOnBuilds);
				Menu.SetChecked(FOLDER_EDITOR_STRIP_MENU_PATH, instance.StripInEditor);
			};
		}

		internal void Save() => Save(SAVE_AS_TEXT);

		[MenuItem(FOLDER_BUILD_STRIP_MENU_PATH, false, 1000)]
		internal static void ToggleBuildFolderStripping() {
			instance.StripOnBuilds = !instance.StripOnBuilds;
			Menu.SetChecked(FOLDER_BUILD_STRIP_MENU_PATH, instance.StripOnBuilds);

			if (EditorWindow.HasOpenInstances<SettingsWindow>())
				EditorWindow.GetWindow<SettingsWindow>().RecreateGUI();

			instance.Save(SAVE_AS_TEXT);
		}

		[MenuItem(FOLDER_EDITOR_STRIP_MENU_PATH, false, 1001)]
		internal static void ToggleEditorFolderStripping() {
			instance.StripInEditor = !instance.StripInEditor;
			Menu.SetChecked(FOLDER_EDITOR_STRIP_MENU_PATH, instance.StripInEditor);

			if (EditorWindow.HasOpenInstances<SettingsWindow>())
				EditorWindow.GetWindow<SettingsWindow>().RecreateGUI();

			instance.Save(SAVE_AS_TEXT);
		}

		[InitializeOnLoadMethod]
		private static void OnLoad() {
			instance.Init();
		}
	}
}
