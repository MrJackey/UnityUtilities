using System;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Conditions;
using UnityEngine;

namespace Jackey.Behaviours.FSM {
	[Serializable]
	public class StateTransition {
		[HideInInspector]
		[SerializeReference] private BehaviourState m_to;
		[SerializeField] private StateTransitionMode m_mode;

		[ShowIf(nameof(m_mode), IfAttribute.Comparison.Equal, (int)StateTransitionMode.OnFinish)]
		[SerializeField] private ActionResult m_finishResult;

		[Space]
		[SerializeField] private BehaviourConditionGroup m_conditions;

		public BehaviourState To => m_to;

		public void Enable(BehaviourOwner owner) {
			m_conditions.Enable(owner);
		}

		public void Disable() {
			m_conditions.Disable();
		}

		public bool Evaluate(BehaviourState state) {
			return m_mode switch {
				StateTransitionMode.OnTick => m_conditions.Evaluate(),
				StateTransitionMode.OnFinish => state.IsFinished && m_conditions.Evaluate(),
				StateTransitionMode.OnSuccess => state.Status == ActionStatus.Success && m_conditions.Evaluate(),
				StateTransitionMode.OnFailure => state.Status == ActionStatus.Failure && m_conditions.Evaluate(),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}
	}
}
