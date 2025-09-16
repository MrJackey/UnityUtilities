using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Bool")]
	public class CompareBool : BehaviourCondition {
		public BlackboardOnlyRef<bool> Comparand;
		public BlackboardRef<bool> Value;

		public override string Editor_Info => $"{Comparand.Editor_Info} is {Value.Editor_Info}";

		public override bool Evaluate() => Comparand.GetValue() == Value.GetValue();
	}
}
