using System;
using System.Collections.Generic;
using System.IO;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEngine;

namespace Jackey.GlobalReferences.Editor.Database {
	public class GlobalObjectDatabase : ScriptableSingleton<GlobalObjectDatabase> {
		private const string ASSETS_FOLDER_GUID = "167818e7901db224ca36d176a667b2d1";
		private const string DATA_FOLDER_NAME = ".Data";

		private static SerializedObject s_serializedObject;
		private static SerializedProperty s_assetsProperty;

		public static SerializedObject SerializedObject => s_serializedObject ??= new SerializedObject(instance);
		public static SerializedProperty AssetsProperty => s_assetsProperty ??= SerializedObject.FindProperty(nameof(Assets));

		[SerializeField] private int m_version;

		public List<GlobalObjectAsset> Assets = new();

		public static event Action AssetsLoaded;

		[InitializeOnLoadMethod]
		public static void Initialize() {
			string dataPath = GetDataPath();
			if (!Directory.Exists(dataPath))
				Directory.CreateDirectory(dataPath);

			if (Directory.GetFiles(dataPath).Length != instance.Assets.Count)
				LoadAllAssets();

			Undo.undoRedoPerformed += OnUndoRedo;
		}

		[MenuItem("Tools/Jackey/Global References/Refresh Assets", priority = 1011)]
		public static void LoadAllAssets() {
			instance.Assets.Clear();

			Undo.ClearUndo(instance);
			instance.m_version = 0;
			DatabaseHistory.instance.Reset();

			string[] filePaths = Directory.GetFiles(GetDataPath());
			try {
				for (int i = 0; i < filePaths.Length; i++) {
					EditorUtility.DisplayProgressBar("Global Object Database", $"Loading all assets: {i + 1} of {filePaths.Length}", i / (float)filePaths.Length);
					string fileContent = File.ReadAllText(filePaths[i]);

					int firstBreak = fileContent.IndexOf('\n');
					int secondBreak = fileContent.IndexOf('\n', firstBreak + 1);

					GlobalObjectAsset asset = new GlobalObjectAsset() {
						GUIDString = fileContent[..firstBreak],
						Name = fileContent[(firstBreak + 1)..secondBreak],
						Description = fileContent[(secondBreak + 1)..],
					};

					instance.Assets.Add(asset);
				}
			}
			finally {
				EditorUtility.ClearProgressBar();
			}

			AssetsLoaded?.Invoke();
		}

		public static GlobalObjectAsset CreateAsset() {
			RecordUndo("Create GlobalObject");

			SerializedGUID guid = SerializedGUID.Generate();
			GlobalObjectAsset asset = new GlobalObjectAsset() {
				GUIDString = guid.ToString(),
				Name = "new GlobalObject",
			};

			DatabaseHistory.instance.RecordChange(new DatabaseHistory.Change() {
				Kind = DatabaseHistory.ChangeKind.Create,
				Guid = asset.GUID,
				Index = instance.Assets.Count,
			}, ++instance.m_version);

			instance.Assets.Add(asset);
			SaveAsset(asset);

			return asset;
		}

		public static void UpdateAsset(GlobalObjectAsset asset) {
			int assetIndex = instance.Assets.IndexOf(asset);
			Debug.Assert(assetIndex != -1);

			DatabaseHistory.instance.RecordChange(new DatabaseHistory.Change() {
				Kind = DatabaseHistory.ChangeKind.Update,
				Index = assetIndex,
			}, ++instance.m_version);
			SaveAsset(asset);
		}

		public static void DeleteAsset(GlobalObjectAsset asset) {
			int assetIndex = instance.Assets.IndexOf(asset);
			Debug.Assert(assetIndex != -1);
			DatabaseHistory.instance.RecordChange(new DatabaseHistory.Change() {
				Kind = DatabaseHistory.ChangeKind.Delete,
				Guid = asset.GUID,
				Index = assetIndex,
			}, ++instance.m_version);

			instance.Assets.Remove(asset);
			RemoveAsset(asset);
		}

		private static void SaveAsset(GlobalObjectAsset asset) {
			File.WriteAllText(GetAssetPath(asset), string.Join('\n', asset.GUIDString, asset.Name, asset.Description));
		}

		private static void RemoveAsset(GlobalObjectAsset asset) {
			File.Delete(GetAssetPath(asset));
		}

		private static void RemoveAsset(SerializedGUID guid) {
			string dataPath = GetDataPath();
			string assetPath = Path.Join(dataPath, $"{guid.ToString()}");
			File.Delete(assetPath);
		}

		public static bool TryGetAsset(SerializedGUID guid, out GlobalObjectAsset result) {
			if (guid == default) {
				result = null;
				return false;
			}

			foreach (GlobalObjectAsset asset in instance.Assets) {
				if (asset.GUID == guid) {
					result = asset;
					return true;
				}
			}

			result = null;
			return false;
		}

		public static void RecordUndo(string name) {
			Undo.RegisterCompleteObjectUndo(instance, name);
		}

		private static void OnUndoRedo() {
			if (DatabaseHistory.instance.Version == instance.m_version) return;

			// Undo
			if (DatabaseHistory.instance.Version > instance.m_version) {
				DatabaseHistory.Change change = DatabaseHistory.instance.Changes[instance.m_version];

				switch (change.Kind) {
					case DatabaseHistory.ChangeKind.Create:
						RemoveAsset(change.Guid);
						break;
					case DatabaseHistory.ChangeKind.Update:
					case DatabaseHistory.ChangeKind.Delete:
						SaveAsset(instance.Assets[change.Index]);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else { // Redo
				DatabaseHistory.Change change = DatabaseHistory.instance.Changes[instance.m_version - 1];

				switch (change.Kind) {
					case DatabaseHistory.ChangeKind.Create:
					case DatabaseHistory.ChangeKind.Update:
						SaveAsset(instance.Assets[change.Index]);
						break;
					case DatabaseHistory.ChangeKind.Delete:
						RemoveAsset(change.Guid);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			DatabaseHistory.instance.Version = instance.m_version;
		}

		private static string GetAssetPath(GlobalObjectAsset asset) {
			string dataPath = GetDataPath();
			return Path.Join(dataPath, $"{asset.GUIDString}");
		}

		private static string GetDataPath() {
			string assetsPath = AssetDatabase.GUIDToAssetPath(ASSETS_FOLDER_GUID);
			return Path.Join(assetsPath, DATA_FOLDER_NAME);
		}
	}
}
