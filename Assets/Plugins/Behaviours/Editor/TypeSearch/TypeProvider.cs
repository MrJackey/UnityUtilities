using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Jackey.Behaviours.Editor.TypeSearch {
	public class TypeProvider : ScriptableObject, ISearchWindowProvider {
		public static Type[] StandardTypes = {
			typeof(bool), typeof(string), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(Enum),
			typeof(Vector2), typeof(Vector2Int), typeof(Vector3), typeof(Vector3Int), typeof(Vector4), typeof(Quaternion),
			typeof(Rect), typeof(RectInt), typeof(Bounds), typeof(BoundsInt),
			typeof(Color), typeof(Gradient), typeof(AnimationCurve), typeof(Hash128), typeof(LayerMask), typeof(Space),
			typeof(GameObject), typeof(Transform),
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
		private List<SearchEntry> m_entries = new();
		private HashSet<string> m_groups = new();

		private TypeCache.TypeCollection m_collection;
		private IList<Type> m_list;
		private Action<Type> m_callback;

		public void AskForType(TypeCache.TypeCollection types, Action<Type> callback) {
			m_collection = types;
			m_list = null;
			m_callback = callback;

			SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), this);
		}

		public void AskForType(IList<Type> list, Action<Type> callback) {
			m_collection = default;
			m_list = list;
			m_callback = callback;

			SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), this);
		}

		public void AskForType(IList<Type> list, TypeCache.TypeCollection collection, Action<Type> callback) {
			m_collection = collection;
			m_list = list;
			m_callback = callback;

			SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), this);
		}

		List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context) {
			m_tree.Clear();
			m_tree.Add(new SearchTreeGroupEntry(s_headerContent));

			m_entries.Clear();
			m_entries.AddRange(m_collection
				.Where(type => !type.IsAbstract)
				.Select(type => new SearchEntry() {
					Type = type,
					Path = type.FullName.Replace('.', '/'), // TODO: Check user category attribute
				}));

			if (m_list != null) {
				m_entries.AddRange(m_list
					.Where(type => !type.IsAbstract)
					.Select(type => new SearchEntry() {
						Type = type,
						Path = type.FullName.Replace('.', '/'), // TODO: Check user category attribute
					}));
			}

			m_entries.Sort((lhs, rhs) => {
				string[] lhsPath = lhs.Path?.Split('/') ?? Array.Empty<string>();
				string[] rhsPath = rhs.Path?.Split('/') ?? Array.Empty<string>();

				for (int i = 0; i < lhsPath.Length; i++) {
					if (i >= rhsPath.Length)
						return 1;

					int comparison = lhsPath[i].CompareTo(rhsPath[i]);

					if (comparison != 0) {
						// Groups go first
						if (lhsPath.Length > rhsPath.Length)
							return -1;

						return comparison;
					}
				}

				return 0;
			});

			m_groups.Clear();

			foreach (SearchEntry entry in m_entries) {
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

		private struct SearchEntry {
			public Type Type;
			public string Path;
		}
	}
}
