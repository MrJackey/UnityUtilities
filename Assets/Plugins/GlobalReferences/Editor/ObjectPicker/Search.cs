using System;
using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;

namespace Jackey.GlobalReferences.Editor.ObjectPicker {
	public static class Search {
		private static readonly List<float> s_scores = new();

		public static void Execute(List<GlobalObjectAsset> input, List<GlobalObjectAsset> output, string search) {
			string[] terms = search.Split(' ');

			s_scores.Clear();
			output.Clear();

			foreach (GlobalObjectAsset asset in input) {
				float score = 0f;

				foreach (string term in terms) {
					if (string.IsNullOrEmpty(term)) continue;

					int index = asset.Name.IndexOf(term, StringComparison.InvariantCultureIgnoreCase);
					if (index == -1) continue;

					score += 1f + (1f - (float)index / asset.Name.Length);
				}

				if (score > 0f) {
					for (int i = 0; i < s_scores.Count; i++) {
						if (score < s_scores[i]) continue;

						s_scores.Insert(i, score);
						output.Insert(i, asset);
						goto continueOuter;
					}

					s_scores.Add(score);
					output.Add(asset);
				}

				continueOuter:;
			}
		}
	}
}
