using System;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Operations {
	[Serializable]
	public class BehaviourOperationList {
		[SerializeReference] private BehaviourOperation[] m_operations = Array.Empty<BehaviourOperation>();

#if UNITY_EDITOR
		public string Editor_Info {
			get {
				if (m_operations.Length == 0)
					return $"{InfoUtilities.AlignCenter("<b>No Operations</b>")}";

				string output = "";

				for (int i = 0; i < m_operations.Length; i++) {
					string operationInfo = m_operations[i].Editor_Info;

					if (string.IsNullOrEmpty(operationInfo))
						operationInfo = m_operations[i].GetType().Editor_GetDisplayOrTypeName();

					if (i < m_operations.Length - 1)
						output += $"{InfoUtilities.MULTI_INFO_SEPARATOR} {operationInfo}\n";
					else
						output += $"{InfoUtilities.MULTI_INFO_SEPARATOR} {operationInfo}";
				}

				return output;
			}
		}
#endif

		public void Execute(BehaviourOwner owner) {
			foreach (BehaviourOperation operation in m_operations) {
				operation.Execute(owner);
			}
		}

		internal void Add(BehaviourOperation operation) {
			Array.Resize(ref m_operations, m_operations.Length + 1);
			m_operations[^1] = operation;
		}
	}
}
