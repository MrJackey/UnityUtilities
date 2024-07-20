using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public class Blackboard {
#if UNITY_EDITOR
		[ItemCanBeNull]
		public static Blackboard[] Available = new Blackboard[2];
#endif

		[SerializeField] internal List<BlackboardVar> m_variables = new();

		internal void MergeInto(Blackboard other) {
			foreach (BlackboardVar var in m_variables) {
				other.m_variables.Add(var);
			}
		}

		public bool TryGetVariable(string name, out BlackboardVar variable) {
			foreach (BlackboardVar var in m_variables) {
				if (var.Name == name) {
					variable = var;
					return true;
				}
			}

			variable = null;
			return false;
		}

		public void SetVariable<T>(string name, T value) {
			foreach (BlackboardVar variable in m_variables) {
				if (variable.Name == name && variable.GetSerializedType() is T) {
					variable.SetValue(value);
					return;
				}
			}

			BlackboardVar newVar = new BlackboardVar(typeof(T));
			newVar.SetValue(value);
			m_variables.Add(newVar);
		}

		[CanBeNull]
		internal BlackboardVar FindVariableWithGuidOrName(string guid, string name) {
			int variableCount = m_variables.Count;

			// Prioritize the guid
			for (int i = 0; i < variableCount; i++) {
				BlackboardVar variable = m_variables[i];
				if (variable.Guid == guid)
					return variable;
			}

			// Then the name
			for (int i = 0; i < variableCount; i++) {
				BlackboardVar variable = m_variables[i];
				if (variable.Name == name)
					return variable;
			}

			return null;
		}
	}
}
