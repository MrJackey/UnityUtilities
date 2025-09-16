using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Utilities;
using Jackey.Behaviours.Variables;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Int")]
	public class CompareInt : BehaviourCondition {
		public BlackboardOnlyRef<int> Comparand;
		public Arithmetic.Comparison Comparison;
		public BlackboardRef<int> Value;

		public override string Editor_Info => $"{Comparand.Editor_Info} {Arithmetic.GetComparisonString(Comparison)} {Value.Editor_Info}";

		public override bool Evaluate() => Arithmetic.Compare(Comparand.GetValue(), Comparison, Value.GetValue());
	}
}
