using System;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Core.Operations {
	[Serializable]
	public class OperationList {
		[SerializeReference] private Operation[] m_operations;

#if UNITY_EDITOR
		public string Editor_Info {
			get {
				string output = "";

				if (m_operations != null) {
					for (int i = 0; i < m_operations.Length; i++) {
						string operationInfo = m_operations[i].Editor_Info;

						if (string.IsNullOrEmpty(operationInfo))
							operationInfo = m_operations[i].GetType().GetDisplayOrTypeName();

						if (i < m_operations.Length - 1)
							output += $"{InfoUtilities.MULTI_INFO_SEPARATOR} {operationInfo}\n";
						else
							output += $"{InfoUtilities.MULTI_INFO_SEPARATOR} {operationInfo}";
					}
				}

				return output;
			}
		}
#endif

		public void Execute(BehaviourOwner owner) {
			foreach (Operation operation in m_operations) {
				operation.Execute(owner);
			}
		}
	}
}
