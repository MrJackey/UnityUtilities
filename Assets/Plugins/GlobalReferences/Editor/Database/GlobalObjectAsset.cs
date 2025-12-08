using System;
using Jackey.GlobalReferences.Utilities;

namespace Jackey.GlobalReferences.Editor.Database {
	[Serializable]
	public class GlobalObjectAsset  {
		public string GUIDString;
		public string Name;
		public string Description;

		private SerializedGUID m_guid;

		public SerializedGUID GUID {
			get {
				if (m_guid != default)
					return m_guid;

				SerializedGUID.TryParse(GUIDString, out m_guid);
				return m_guid;
			}
		}
	}
}
