using System;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public struct BlackboardOnlyRef<T> : IBlackboardRef, ISerializationCallbackReceiver {
		[SerializeField] private ObjectBehaviour m_behaviour;

		[SerializeField] private SerializedGUID m_variableGuid;
		[SerializeField] private string m_variableName;

		private BlackboardVar m_cachedVariable;

		public bool IsEmpty => m_variableGuid == default;

		public string Editor_Info => IBlackboardRef.Editor_VariableInfo<T>(m_behaviour, m_variableGuid, m_variableName, ref m_cachedVariable);

		public T GetValue() => IBlackboardRef.GetVariableValue<T>(m_behaviour, m_variableGuid, m_variableName, ref m_cachedVariable);
		public void SetValue(T value) => IBlackboardRef.SetVariableValue(value, m_behaviour, m_variableGuid, m_variableName, ref m_cachedVariable);

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			if (m_variableGuid == default) {
				m_variableName = null;
				return;
			}

			BlackboardVar variable = IBlackboardRef.FindReferencedVariable<T>(m_behaviour, m_variableGuid, m_variableName);
			if (variable != null && variable.IsAssignableTo(typeof(T))) {
				m_variableGuid = variable.Guid;
				m_variableName = variable.Name;
			}
		}
		void ISerializationCallbackReceiver.OnAfterDeserialize() { }
	}
}
