using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Utilities;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Conditions.Comparisons {
	[SearchPath("Utilities/Compare Distance 2D")]
	public class CompareDistance2D : BehaviourCondition {
		[SerializeField] private BlackboardRef<Vector2> m_from;
		[SerializeField] private BlackboardRef<Vector2> m_to;
		[SerializeField] private Arithmetic.Comparison m_comparison;
		[SerializeField] private BlackboardRef<float> m_comparand;

#if UNITY_EDITOR
		public override string Editor_Info => $"{m_from.Editor_Info} ⇤⇥ {m_to.Editor_Info} {Arithmetic.GetComparisonString(m_comparison)} {m_comparand.Editor_Info}";
#endif

		public override bool Evaluate() {
			float comparand = m_comparand.GetValue();
			return Arithmetic.Compare(Vector2.SqrMagnitude(m_to.GetValue() - m_from.GetValue()), m_comparison, comparand * comparand);
		}
	}
}
