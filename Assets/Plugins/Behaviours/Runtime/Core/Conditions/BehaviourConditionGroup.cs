using System;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Core.Conditions {
	[Serializable]
	public class BehaviourConditionGroup {
		[SerializeField] private Policy m_policy;
		[SerializeReference] internal BehaviourCondition[] m_conditions = Array.Empty<BehaviourCondition>();
		[SerializeField] private bool m_invert;

#if UNITY_EDITOR
		public string Editor_Info {
			get {
				if (m_conditions.Length == 0)
					return $"{InfoUtilities.AlignCenter("<b>No Conditions</b>")}";

#if UNITY_EDITOR
				string policyString = UnityEditor.ObjectNames.NicifyVariableName(m_policy.ToString());
#else
				string policyString = m_policy.ToString();
#endif
				string output = InfoUtilities.AlignCenter(m_invert ? $"<b>not {policyString}</b>\n" : $"<b>{policyString}</b>\n");

				for (int i = 0; i < m_conditions.Length; i++) {
					string conditionInfo = m_conditions[i].Editor_Info;

					if (string.IsNullOrEmpty(conditionInfo))
						conditionInfo = m_conditions[i].GetType().Editor_GetDisplayOrTypeName();

					if (i < m_conditions.Length - 1)
						output += $"{InfoUtilities.MULTI_INFO_SEPARATOR} {conditionInfo}\n";
					else
						output += $"{InfoUtilities.MULTI_INFO_SEPARATOR} {conditionInfo}";
				}

				return output;
			}
		}
#endif

	public void Enable(BehaviourOwner owner) {
		int conditionCount = m_conditions.Length;
		for (int i = 0; i < conditionCount; i++) {
			m_conditions[i].Enable(owner);
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
