using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.Utilities.Unity {
	/// <summary>
	/// Save a reference to an asset located within a "Resources/" folder
	/// </summary>
	[Serializable]
	public struct ResourceReference<T> : ISerializationCallbackReceiver, IEquatable<ResourceReference<T>> where T : Object {
#if UNITY_EDITOR
		[SerializeField] private string m_assetGuid;
#endif
		[SerializeField] private string m_resourcePath;

		/// <summary>
		/// The path to the referenced asset following its 'Resources/' folder
		/// </summary>
		public string ResourcePath {
			get {
#if UNITY_EDITOR
				return GetResourcePath();
#else
				return m_resourcePath;
#endif
			}
		}

		/// <summary>
		/// Is the reference valid?
		/// </summary>
		public bool IsValid {
			get {
#if UNITY_EDITOR
				return !string.IsNullOrEmpty(GetResourcePath());
#else
				return !string.IsNullOrEmpty(m_resourcePath);
#endif
			}
		}

		#region Serialization

		public void OnBeforeSerialize() {
#if UNITY_EDITOR
			m_resourcePath = GetResourcePath();
#endif
		}

		public void OnAfterDeserialize() { }

		#endregion

		/// <summary>
		/// Load the referenced asset
		/// </summary>
		/// <returns>Returns the loaded asset</returns>
		public T Load() {
			if (!IsValid) {
				Debug.LogWarning("Unable to load resource. Reference is invalid");
				return null;
			}

#if UNITY_EDITOR
			return Resources.Load<T>(GetResourcePath());
#else
			return Resources.Load<T>(m_resourcePath);
#endif
		}

		/// <summary>
		/// Begin asynchronous loading of the referenced asset
		/// </summary>
		/// <returns>Returns the asset load request</returns>
		public ResourceRequest LoadAsync() {
			if (!IsValid) {
				Debug.LogWarning("Unable to load resource. Reference is invalid");
				return null;
			}

#if UNITY_EDITOR
			return Resources.LoadAsync<T>(GetResourcePath());
#else
			return Resources.LoadAsync<T>(m_resourcePath);
#endif
		}

#if UNITY_EDITOR
		/// <summary>
		/// Create a new resource reference. This method can only ever be called in the Editor!
		/// </summary>
		/// <param name="asset">The asset to create a reference to</param>
		/// <returns>A reference to the asset via its resource path. Note that the reference may be invalid if the asset is not in a Resources/ folder</returns>
		public static ResourceReference<T> Create(T asset) {
			return new ResourceReference<T>() {
				m_assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)),
			};
		}
#endif

#if UNITY_EDITOR
		private string GetResourcePath() {
			string assetPath;

			if (string.IsNullOrEmpty(m_assetGuid) || string.IsNullOrEmpty(assetPath = AssetDatabase.GUIDToAssetPath(m_assetGuid))) {
				return string.Empty;
			}

			string directoryName = Path.GetDirectoryName(assetPath);
			string fileName = Path.GetFileNameWithoutExtension(assetPath);
			string assetPathWithoutExtension = Path.Combine(directoryName, fileName);

			assetPathWithoutExtension = assetPathWithoutExtension.Replace('\\', '/');

			return Regex.Match(assetPathWithoutExtension, "(?<=/Resources/).+$").Value;
		}
#endif

		public override string ToString() {
#if UNITY_EDITOR
			return GetResourcePath();
#else
			return m_resourcePath;
#endif
		}

		public static bool operator ==(ResourceReference<T> lhs, ResourceReference<T> rhs) => lhs.Equals(rhs);
		public static bool operator !=(ResourceReference<T> lhs, ResourceReference<T> rhs) => !lhs.Equals(rhs);

		public override bool Equals(object obj) => obj is ResourceReference<T> other && Equals(other);
		public bool Equals(ResourceReference<T> other) {
#if UNITY_EDITOR
			return m_assetGuid == other.m_assetGuid;
#else
			return m_resourcePath == other.m_resourcePath;
#endif
		}

		public override int GetHashCode() {
#if UNITY_EDITOR
			return GetResourcePath().GetHashCode();
#else
			return m_resourcePath.GetHashCode();
#endif
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(ResourceReference<>))]
		public class ResourceReferencePropertyDrawer : PropertyDrawer {
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				Color defaultGUIBackgroundColor = GUI.backgroundColor;

				label = EditorGUI.BeginProperty(position, label, property);

				SerializedProperty guidProperty = property.FindPropertyRelative("m_assetGuid");
				string assetGuid = guidProperty.stringValue;

				Type assetType = fieldInfo.FieldType.GenericTypeArguments[0];
				Object asset = null;

				if (!string.IsNullOrEmpty(assetGuid)) {
					string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
					asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);

					if (!Regex.IsMatch(assetPath, "(?<=/Resources/).+$")) {
						GUI.backgroundColor = Color.red;
						label.tooltip = "Asset is not located within a \"Resources/\" folder";
					}
				}

				EditorGUI.BeginChangeCheck();
				Object fieldValue = EditorGUI.ObjectField(position, label, asset, assetType, false);
				if (EditorGUI.EndChangeCheck()) {
					string assetPath = AssetDatabase.GetAssetPath(fieldValue);
					guidProperty.stringValue = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
				}

				EditorGUI.EndProperty();

				GUI.backgroundColor = defaultGUIBackgroundColor;
			}
		}
	}
#endif
}
