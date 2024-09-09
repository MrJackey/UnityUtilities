using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Operations;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Operator")]
	public class Operator : Decorator {
		[Header("On Enter")]
		[SerializeField] private OperationList m_enterOperations;

		[Header("On Exit")]
		[SerializeField] private OperationList m_exitOperations;

#if UNITY_EDITOR
		public override string Editor_Info => $"<b>On Enter</b>\n{m_enterOperations?.Editor_Info}\n\n<b>On Exit</b>\n{m_exitOperations?.Editor_Info}";
#endif

		protected override ExecutionStatus OnTick() {
			if (!m_child.IsFinished) {
				m_enterOperations.Execute(Owner);
				return m_child.EnterSequence();
			}

			return (ExecutionStatus)m_child.Status;
		}

		protected override void OnExit() {
			m_exitOperations.Execute(Owner);
		}
	}
}
