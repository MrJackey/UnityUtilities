using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Utilities;
using Jackey.Behaviours.Variables;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Float")]
	public class CompareFloat : BehaviourCondition {
		public BlackboardOnlyRef<float> Comparand;
		public Arithmetic.Comparison Comparison;
		public BlackboardRef<float> Value;

		public override string Editor_Info => $"{Comparand.Editor_Info} {Arithmetic.GetComparisonString(Comparison)} {Value.Editor_Info}";

		public override bool Evaluate() => Arithmetic.Compare(Comparand.GetValue(), Comparison, Value.GetValue());
	}
}
