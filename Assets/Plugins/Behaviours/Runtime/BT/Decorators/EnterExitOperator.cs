using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Operations;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/EnterExit Operator")]
	public class EnterExitOperator : Decorator {
		[Header("On Enter")]
		[SerializeField] private OperationList m_enterOperations;

		[Header("On Exit")]
		[SerializeField] private OperationList m_exitOperations;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("<b>On Enter</b>")}\n" +
		                                      $"{m_enterOperations?.Editor_Info}\n\n" +
		                                      $"{InfoUtilities.AlignCenter("<b>On Exit</b>")}\n" +
		                                      $"{m_exitOperations?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			m_enterOperations.Execute(Owner);
			return m_child.EnterSequence();
		}

		protected override void OnExit() {
			m_exitOperations.Execute(Owner);
		}
	}
}
