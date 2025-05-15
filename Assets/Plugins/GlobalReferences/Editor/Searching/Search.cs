using System;
using System.Collections.Generic;
using Jackey.GlobalReferences.Editor.Database;

namespace Jackey.GlobalReferences.Editor.ObjectPicker {
	public static class Search {
		private static readonly List<float> s_scores = new();

		public static void Execute(List<GlobalObjectAsset> input, List<GlobalObjectAsset> output, string search) {
			List<int> indices = new List<int>();
			Execute(input, asset => asset.Name, search, indices);

			output.Clear();
			foreach (int index in indices)
				output.Add(input[index]);
		}

		public static void Execute<T>(List<T> input, Func<T, string> objectToRange, string search, List<int> output) {
			string[] terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			s_scores.Clear();
			output.Clear();

			for (int inputIndex = 0; inputIndex < input.Count; inputIndex++) {
				T obj = input[inputIndex];

				string range = objectToRange(obj);
				float score = 0f;

				foreach (string term in terms) {
					int index = range.IndexOf(term, StringComparison.InvariantCultureIgnoreCase);
					if (index == -1) continue;

					float matchScore = 1f; // Match
					matchScore += (1f - (float)index / range.Length) * ((float)term.Length / range.Length); // How early it appears and how much of the name is the term

					score += matchScore;
				}

				if (score > 0f) {
					for (int scoreIndex = 0; scoreIndex < s_scores.Count; scoreIndex++) {
						if (score <= s_scores[scoreIndex]) continue;

						s_scores.Insert(scoreIndex, score);
						output.Insert(scoreIndex, inputIndex);
						goto continueOuter;
					}

					s_scores.Add(score);
					output.Add(inputIndex);
				}

				continueOuter: ;
			}
		}
	}
}
