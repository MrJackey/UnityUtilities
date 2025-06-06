﻿using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Utilities;

namespace Jackey.Behaviours.Core.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Int")]
	public class CompareInt : BehaviourCondition {
		public BlackboardOnlyRef<int> Comparand;
		public Arithmetic.Comparison Comparison;
		public BlackboardRef<int> Value;

		public override string Editor_Info => $"{Comparand.Editor_Info} {Arithmetic.GetComparisonString(Comparison)} {Value.Editor_Info}";

		public override bool Evaluate() => Arithmetic.Compare(Comparand.GetValue(), Comparison, Value.GetValue());
	}
}
