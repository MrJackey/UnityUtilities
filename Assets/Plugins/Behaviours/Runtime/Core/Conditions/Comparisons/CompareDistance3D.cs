using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Utilities;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Utilities/Compare Vector3 Distance")]
	public class CompareDistance3D : BehaviourCondition {
		public BlackboardRef<Vector3> From;
		public BlackboardRef<Vector3> To;
		public Arithmetic.Comparison Comparison;
		public BlackboardRef<float> Comparand;

		public override string Editor_Info => $"{From.Editor_Info} ⇤⇥ {To.Editor_Info} {Arithmetic.GetComparisonString(Comparison)} {Comparand.Editor_Info}";

		public override bool Evaluate() {
			float comparand = Comparand.GetValue();
			return Arithmetic.Compare(Vector3.SqrMagnitude(To.GetValue() - From.GetValue()), Comparison, comparand * comparand);
		}
	}
}
