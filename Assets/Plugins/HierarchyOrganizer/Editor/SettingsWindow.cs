using Jackey.HierarchyOrganizer.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.HierarchyOrganizer.Editor {
	internal class SettingsWindow : EditorWindow {
		[MenuItem("Tools/Jackey/Hierarchy Organizer/Settings", false, 1020)]
		private static void ShowWindow() {
			SettingsWindow window = GetWindow<SettingsWindow>();
			window.Show();
		}

		private void CreateGUI() {
			titleContent.text = $"{PluginInfo.NAME} Settings";

			CreateFolderGUI();

			rootVisualElement.Add(Space(12f));

			CreateIndentGuideGUI();
		}

		private void CreateFolderGUI() {
			VisualElement folderSection = CreateSection("Folders");

			Toggle stripBuildsField = new Toggle(ObjectNames.NicifyVariableName(nameof(Settings.instance.StripOnBuilds))) {
				value = Settings.instance.StripOnBuilds,
			};
			stripBuildsField.RegisterValueChangedCallback(evt => {
				Settings.instance.StripOnBuilds = evt.newValue;
				Settings.instance.Save();
			});
			folderSection.Add(stripBuildsField);

			Toggle warnField = new Toggle("Warn on Builds") {
				value = Settings.instance.WarnOfDisabledStripBuilds,
				tooltip = "If build stripping is disabled, a dialog will appear on builds allowing for an exception for that specific build",
			};
			warnField.RegisterValueChangedCallback(evt => {
				Settings.instance.WarnOfDisabledStripBuilds = evt.newValue;
				Settings.instance.Save();
			});
			folderSection.Add(warnField);

			folderSection.Add(Space(4f));

			Toggle stripEditorField = new Toggle(ObjectNames.NicifyVariableName(nameof(Settings.instance.StripInEditor))) {
				value = Settings.instance.StripInEditor,
			};
			stripEditorField.RegisterValueChangedCallback(evt => {
				Settings.instance.StripInEditor = evt.newValue;
				Settings.instance.Save();
			});
			folderSection.Add(stripEditorField);

			folderSection.Add(Space(6f));

			EnumField disabledFolderMethodField = new EnumField("Disabled Folder Strip", Settings.instance.DisabledFolderStripMethod) {
				tooltip = "What should happen with a folder's content when it's disabled and stripped from the scene",
			};
			disabledFolderMethodField.RegisterValueChangedCallback(evt => {
				Settings.instance.DisabledFolderStripMethod = (HierarchyFolderProcessor.DisabledFolderStripMethod)evt.newValue;
				Settings.instance.Save();
			});
			folderSection.Add(disabledFolderMethodField);

			rootVisualElement.Add(folderSection);
		}

		private void CreateIndentGuideGUI() {
			VisualElement indentGuideSection = CreateSection("Indent Guide");

			ColorField colorField = new ColorField("Color") {
				value = Settings.instance.IndentGuideColor,
			};
			colorField.RegisterValueChangedCallback(evt => {
				Settings.instance.IndentGuideColor = evt.newValue;
				Settings.instance.Save();
			});

			EnumField enumField = new EnumField("Draw Mode", Settings.instance.IndentGuide);
			enumField.RegisterValueChangedCallback(evt => {
				colorField.style.display = (HierarchyDrawer.IndentGuide)evt.newValue != HierarchyDrawer.IndentGuide.None
					? DisplayStyle.Flex
					: DisplayStyle.None;

				Settings.instance.IndentGuide = (HierarchyDrawer.IndentGuide)evt.newValue;
				Settings.instance.Save();
			});
			indentGuideSection.Add(enumField);

			indentGuideSection.Add(colorField);

			rootVisualElement.Add(indentGuideSection);
		}

		private VisualElement CreateSection(string header) {
			VisualElement section = new() {
				style = {
					paddingBottom = EditorGUIUtility.standardVerticalSpacing,
					paddingTop = EditorGUIUtility.standardVerticalSpacing,
				},
			};

			section.Add(new Label(header) {
				style = {
					marginLeft = 3f,
					marginBottom = EditorGUIUtility.standardVerticalSpacing,
					unityTextAlign = TextAnchor.MiddleLeft,
					unityFontStyleAndWeight = FontStyle.Bold,
				},
			});

			return section;
		}

		private VisualElement Space(float space) {
			return new VisualElement() {
				style = {
					height = space,
					width = new Length(100f, LengthUnit.Percent),
				},
			};
		}

		internal void RecreateGUI() {
			rootVisualElement.Clear();
			CreateGUI();
		}
	}
}
