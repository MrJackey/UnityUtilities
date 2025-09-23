using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Conditions {
	[DisplayName("Check Random")]
	[SearchPath("Utilities/Check Random")]
	public class CheckRandom : BehaviourCondition {
		[SerializeField] private BlackboardRef<float> m_probability;

#if UNITY_EDITOR
		public override string Editor_Info => m_probability.IsValue
			? Mathf.Clamp01(m_probability.GetValue()).ToString("P")
			: $"{m_probability.Editor_Info}%";
#endif

		public override bool Evaluate() {
			float probability = m_probability.GetValue();
			return probability > 0f && Random.value <= probability;
		}
	}
}
