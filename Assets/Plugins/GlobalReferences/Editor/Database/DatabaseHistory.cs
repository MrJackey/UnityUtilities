using System;
using System.Collections.Generic;
using Jackey.GlobalReferences.Utilities;
using UnityEditor;
using UnityEngine;

namespace Jackey.GlobalReferences.Editor.Database {
	internal class DatabaseHistory : ScriptableSingleton<DatabaseHistory> {
		[SerializeField] private int m_version;

		public List<Change> Changes = new();
		public int Version {
			get => m_version;
			set => m_version = value;
		}

		public void RecordChange(Change change, int version) {
			m_version = version;

			// Remove "future" changes
			for (int i = Changes.Count - 1; i >= m_version - 1; i--)
				Changes.RemoveAt(i);

			Changes.Add(change);
		}

		public void Reset() {
			instance.Version = 0;
			instance.Changes.Clear();
		}

		[Serializable]
		public struct Change {
			public ChangeKind Kind;
			public SerializedGUID Guid;
			public int Index;
		}

		public enum ChangeKind {
			Create,
			Update,
			Delete,
		}
	}
}
