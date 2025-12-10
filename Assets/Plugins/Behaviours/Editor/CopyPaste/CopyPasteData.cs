using System;
using UnityEngine;

namespace Jackey.Behaviours.Editor.CopyPaste {
	[Serializable]
	public class CopyPasteData {
		public CopyPasteContext Context;
		public string Data;

		public static bool TryParse<T>(CopyPasteContext ctx, out T data) {
			data = default;

			CopyPasteData pasteData;
			try {
				pasteData = JsonUtility.FromJson<CopyPasteData>(GUIUtility.systemCopyBuffer);
			}
			catch (ArgumentException) {
				return false;
			}

			if (pasteData == null || pasteData.Context != ctx)
				return false;

			try {
				data = JsonUtility.FromJson<T>(pasteData.Data);
			}
			catch (ArgumentException) {
				return false;
			}

			return true;
		}
	}
}
