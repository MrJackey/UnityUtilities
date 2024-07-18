using System;
using UnityEngine;

namespace Jackey.Behaviours.Core.Conditions {
	[Serializable]
	public class BehaviourConditionGroup {
		[SerializeField] private Policy m_policy;
		[SerializeReference] internal BehaviourCondition[] m_conditions;
		[SerializeField] private bool m_invert;

		public string Editor_Info {
			get {
				string output = m_invert ? $"if not {m_policy}\n" : $"if {m_policy}\n";

				if (m_conditions != null) {
					for (int i = 0; i < m_conditions.Length; i++) {
						string conditionInfo = m_conditions[i].Editor_Info;

						if (string.IsNullOrEmpty(conditionInfo))
							conditionInfo = m_conditions[i].GetType().Name;

						if (i < m_conditions.Length - 1)
							output += $"{conditionInfo}\n";
						else
							output += $"{conditionInfo}";
					}
				}

				return output;
			}
		}

	public void Enable(BehaviourOwner owner) {
		int conditionCount = m_conditions.Length;
		for (int i = 0; i < conditionCount; i++) {
			m_conditions[i].OnEnable(owner);
		}
	}

	public void Disable() {
		int conditionCount = m_conditions.Length;
		for (int i = 0; i < conditionCount; i++) {
			m_conditions[i].OnDisable();
		}
	}

	public bool Evaluate() {
			if (m_conditions.Length == 0)
				return true;

			bool result = m_policy switch {
				Policy.AllTrue => EvaluateAll(),
				Policy.AnyTrue => EvaluateAny(),
				_ => throw new ArgumentOutOfRangeException(),
			};

			if (m_invert)
				result = !result;

			return result;
		}

		private bool EvaluateAll() {
			foreach (BehaviourCondition condition in m_conditions) {
				if (!condition.Evaluate()) {
					return false;
				}
			}

			return true;
		}

		private bool EvaluateAny() {
			foreach (BehaviourCondition condition in m_conditions) {
				if (condition.Evaluate()) {
					return true;
				}
			}

			return false;
		}

		private enum Policy {
			AllTrue,
			AnyTrue,
		}
	}
}
