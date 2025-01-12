﻿using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	[SearchPath("Decorators/Wait Until")]
	public class WaitUntil : Decorator {
		[SerializeField] private BehaviourConditionGroup m_conditions;

#if UNITY_EDITOR
		public override string Editor_Info => $"{InfoUtilities.AlignCenter("Wait Until")}\n" +
		                                      $"{m_conditions?.Editor_Info}";
#endif

		protected override ExecutionStatus OnEnter() {
			m_conditions.Enable(Owner);
			EnableTicking();

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnTick() {
			if (m_child?.IsFinished ?? false)
				return (ExecutionStatus)m_child.Status;

			if (m_conditions.Evaluate()) {
				DisableTicking();
				return m_child?.EnterSequence() ?? ExecutionStatus.Success;
			}

			return ExecutionStatus.Running;
		}

		protected override void OnExit() {
			m_conditions.Disable();
		}
	}
}
