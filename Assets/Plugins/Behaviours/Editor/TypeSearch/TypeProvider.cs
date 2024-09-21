using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jackey.Behaviours.Attributes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Jackey.Behaviours.Editor.TypeSearch {
	public class TypeProvider : ScriptableObject, ISearchWindowProvider {
		public static SearchEntry[] StandardTypes = {
			Type2Search("System/Bool", typeof(bool)), Type2Search("System/String", typeof(string)),
			Type2Search("System/Int", typeof(int)), Type2Search("System/uInt", typeof(uint)), Type2Search("System/Long", typeof(long)), Type2Search("System/uLong", typeof(ulong)),
			Type2Search("System/Float", typeof(float)), Type2Search("System/Double", typeof(double)),
			Type2Search("UnityEngine/Vector2", typeof(Vector2)), Type2Search("UnityEngine/Vector2Int", typeof(Vector2Int)), Type2Search("UnityEngine/Vector3", typeof(Vector3)), Type2Search("UnityEngine/Vector3Int", typeof(Vector3Int)), Type2Search("UnityEngine/Vector4", typeof(Vector4)), Type2Search("UnityEngine/Quaternion", typeof(Quaternion)),
			Type2Search("UnityEngine/Rect", typeof(Rect)), Type2Search("UnityEngine/RectInt", typeof(RectInt)), Type2Search("UnityEngine/Bounds", typeof(Bounds)), Type2Search("UnityEngine/BoundsInt", typeof(BoundsInt)),
			Type2Search("UnityEngine/Color", typeof(Color)), Type2Search("UnityEngine/Gradient", typeof(Gradient)), Type2Search("UnityEngine/AnimationCurve", typeof(AnimationCurve)), Type2Search("UnityEngine/Hash128", typeof(Hash128)), Type2Search("UnityEngine/LayerMask", typeof(LayerMask)), Type2Search("UnityEngine/Space", typeof(Space)),
			Type2Search("UnityEngine/GameObject", typeof(GameObject)), Type2Search("UnityEngine/Transform", typeof(Transform)), Type2Search("UnityEngine/Component", typeof(Component)),
		};

		private static TypeProvider s_instance;
		public static TypeProvider Instance {
			get {
				if (s_instance == null)
					s_instance = CreateInstance<TypeProvider>();

				return s_instance;
			}
		}

		private static GUIContent s_headerContent = new GUIContent("Type");

		private List<SearchTreeEntry> m_tree = new();
		private HashSet<string> m_groups = new();

		private IEnumerable<SearchEntry> m_entries;
		private Action<Type> m_callback;

		public static IEnumerable<SearchEntry> TypesToSearch(IEnumerable<Type> types) {
			IEnumerable<SearchEntry> results = types
				.Where(type => !type.IsAbstract)
				.Select(type => {
					string path;

					SearchPathAttribute pathAttribute = type.GetCustomAttribute<SearchPathAttribute>();
					if (pathAttribute != null)
						path = pathAttribute.Path;
					else
						path = type.FullName.Replace('.', '/');

					return new SearchEntry() {
						Type = type,
						Path = path,
					};
				});

			return results;
		}

		public void AskForType(Vector2 screenPosition, IEnumerable<Type> types, Action<Type> callback) => AskForType(screenPosition, TypesToSearch(types), callback);
		public void AskForType(Vector2 screenPosition, IEnumerable<SearchEntry> entries, Action<Type> callback) {
			m_entries = entries;
			m_callback = callback;

			SearchWindow.Open(new SearchWindowContext(screenPosition), this);
		}

		List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context) {
			m_tree.Clear();
			m_tree.Add(new SearchTreeGroupEntry(s_headerContent));

			List<SearchEntry> entryList = m_entries.ToList();

			entryList.Sort((lhs, rhs) => {
				string[] lhsPath = lhs.Path?.Split('/') ?? Array.Empty<string>();
				string[] rhsPath = rhs.Path?.Split('/') ?? Array.Empty<string>();

				// Ensure groups go first
				if (lhsPath.Length == 1 && rhsPath.Length > 1)
					return 1;

				if (rhsPath.Length == 1 && lhsPath.Length > 1)
					return -1;

				for (int i = 0; i < lhsPath.Length; i++) {
					// Equal path until now with lhs still having another group whilst rhs does not one
					if (i >= rhsPath.Length)
						return -1;

					int comparison = string.Compare(lhsPath[i], rhsPath[i], StringComparison.Ordinal);

					if (comparison != 0)
						return comparison;
				}

				return 0;
			});

			m_groups.Clear();

			foreach (SearchEntry entry in entryList) {
				string[] path = entry.Path.Split('/');
				string groupPath = string.Empty;

				for (int i = 0; i < path.Length - 1; i++) {
					groupPath += path[i] + '/';

					if (!m_groups.Contains(groupPath)) {
						m_tree.Add(new SearchTreeGroupEntry(new GUIContent(path[i]), i + 1));
						m_groups.Add(groupPath);
					}
				}

				m_tree.Add(new SearchTreeEntry(new GUIContent(path[^1])) {
					level = path.Length,
					userData = entry.Type,
				});
			}

			return m_tree;
		}

		bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context) {
			m_callback.Invoke((Type)searchTreeEntry.userData);
			return true;
		}

		private static SearchEntry Type2Search(string path, Type type) {
			return new SearchEntry() {
				Type = type,
				Path = path,
			};
		}

		public struct SearchEntry {
			public Type Type;
			public string Path;
		}
	}
}
