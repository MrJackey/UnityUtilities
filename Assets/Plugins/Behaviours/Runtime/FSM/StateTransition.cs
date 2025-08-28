using System;
using System.Collections.Generic;
using System.Linq;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Conditions;
using Jackey.Behaviours.FSM.States;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.FSM {
	[Serializable]
	public class StateTransition {
		[SkipBlackboardConnect, HideInNormalInspector]
		[SerializeReference] private BehaviourState m_destination;
		[SerializeField] private List<StateTransitionGroup> m_groups = new();

#if UNITY_EDITOR
		public string Editor_Info {
			get {
				if (m_groups.Count == 0)
					return "On Finish";

				return string.Join($"\n{InfoUtilities.AlignCenter("———")}\n", m_groups.Select(group => group.Editor_Info));
			}
		}
#endif

		internal BehaviourState Destination {
			get => m_destination;
			set => m_destination = value;
		}

		public void Enable(BehaviourOwner owner) {
			foreach (StateTransitionGroup group in m_groups) {
				group.Enable(owner);
			}
		}

		public void Disable() {
			foreach (StateTransitionGroup group in m_groups) {
				group.Disable();
			}
		}

		public bool Evaluate(StateTransitionContext ctx, out BehaviourState destination) {
			destination = Destination;

			// Default to OnFinish transition with no groups
			if (m_groups.Count == 0 && ctx != StateTransitionContext.OnTick)
				return true;

			foreach (StateTransitionGroup group in m_groups) {
				if (group.Evaluate(ctx))
					return true;
			}

			destination = null;
			return false;
		}
	}

	[Serializable]
	public class StateTransitionGroup {
		[SerializeField] private StateTransitionContext m_context;

		[Header("Conditions")]
		[SerializeField] private BehaviourConditionGroup m_conditions = new();

#if UNITY_EDITOR
		public string Editor_Info => $"{InfoUtilities.AlignCenter(UnityEditor.ObjectNames.NicifyVariableName(m_context.ToString()))}\n{m_conditions.Editor_Info}";
#endif

		public void Enable(BehaviourOwner owner) {
			m_conditions.Enable(owner);
		}

		public void Disable() {
			m_conditions.Disable();
		}

		public bool Evaluate(StateTransitionContext ctx) {
			return m_context switch {
				StateTransitionContext.OnFinish => ctx is StateTransitionContext.OnSuccess or StateTransitionContext.OnFailure,
				StateTransitionContext.OnTick => ctx is StateTransitionContext.OnTick,
				StateTransitionContext.OnSuccess => ctx is StateTransitionContext.OnSuccess,
				StateTransitionContext.OnFailure => ctx is StateTransitionContext.OnFailure,
				_ => throw new ArgumentOutOfRangeException(),
			} && m_conditions.Evaluate();
		}
	}

	public enum StateTransitionContext {
		OnFinish,
		OnTick,
		OnSuccess,
		OnFailure,
	}
}
