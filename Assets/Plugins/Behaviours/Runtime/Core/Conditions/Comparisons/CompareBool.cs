using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Blackboard/Compare Bool")]
	public class CompareBool : BehaviourCondition {
		[SerializeField] private BlackboardOnlyRef<bool> m_comparand;
		[SerializeField] private BlackboardRef<bool> m_value;

#if UNITY_EDITOR
		public override string Editor_Info => $"{m_comparand.Editor_Info} is {m_value.Editor_Info}";
#endif

		public override bool Evaluate() => m_comparand.GetValue() == m_value.GetValue();
	}
}
