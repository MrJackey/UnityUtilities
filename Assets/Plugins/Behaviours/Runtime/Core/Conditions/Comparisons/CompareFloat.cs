using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Utilities;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Float")]
	public class CompareFloat : BehaviourCondition {
		[SerializeField] private BlackboardOnlyRef<float> m_comparand;
		[SerializeField] private Arithmetic.Comparison m_comparison;
		[SerializeField] private BlackboardRef<float> m_value;

#if UNITY_EDITOR
		public override string Editor_Info => $"{m_comparand.Editor_Info} {Arithmetic.GetComparisonString(m_comparison)} {m_value.Editor_Info}";
#endif

		public override bool Evaluate() => Arithmetic.Compare(m_comparand.GetValue(), m_comparison, m_value.GetValue());
	}
}
