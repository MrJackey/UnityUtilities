using Jackey.Behaviours.Core.Blackboard;

namespace Jackey.Behaviours.Core.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Bool")]
	public class CompareBool : BehaviourCondition {
		[BlackboardOnly]
		public BlackboardRef<bool> Comparand;
		public BlackboardRef<bool> Value;

		public override string Editor_Info => $"{Comparand.Editor_Info} is {Value.Editor_Info}";

		public override bool Evaluate() => Comparand.GetValue() == Value.GetValue();
	}
}
