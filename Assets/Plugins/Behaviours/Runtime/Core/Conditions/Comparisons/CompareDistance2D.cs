using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Core.Conditions.Comparisons {
	[SearchPath("Utilities/Compare Vector2 Distance")]
	public class CompareDistance2D : BehaviourCondition {
		public BlackboardRef<Vector2> From;
		public BlackboardRef<Vector2> To;
		public Arithmetic.Comparison Comparison;
		public BlackboardRef<float> Comparand;

		public override string Editor_Info => $"{From.Editor_Info} --> {To.Editor_Info} {Arithmetic.GetComparisonString(Comparison)} {Comparand.Editor_Info}";

		public override bool Evaluate() {
			float comparand = Comparand.GetValue();
			return Arithmetic.Compare(Vector2.SqrMagnitude(To.GetValue() - From.GetValue()), Comparison, comparand * comparand);
		}
	}
}
