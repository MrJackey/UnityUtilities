using System;
using Jackey.Behaviours.Utilities;
using UnityEngine;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public struct BlackboardRef<T> : IBlackboardRef, ISerializationCallbackReceiver {
		[SerializeField] private ObjectBehaviour m_behaviour;

		[SerializeField] private Mode m_mode;
		[SerializeField] private T m_fieldValue;

		[SerializeField] private SerializedGUID m_variableGuid;
		[SerializeField] private string m_variableName;

		private BlackboardVar m_cachedVariable;

		public bool IsValue => m_mode is Mode.Field;
		public bool IsVariable => m_mode is Mode.Variable;
		public bool IsEmptyVariable => m_mode is Mode.Variable && m_variableGuid == default;

		public string Editor_Info {
			get {
				switch (m_mode) {
					case Mode.Field:
						return $"{m_fieldValue?.ToString() ?? string.Empty}";
					case Mode.Variable:
						return IBlackboardRef.Editor_VariableInfo<T>(m_behaviour, m_variableGuid, m_variableName, ref m_cachedVariable);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public T GetValue() {
			return m_mode switch {
				Mode.Field => m_fieldValue,
				Mode.Variable => IBlackboardRef.GetVariableValue<T>(m_behaviour, m_variableGuid, m_variableName, ref m_cachedVariable),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		public void SetValue(T value) {
			switch (m_mode) {
				case Mode.Field:
					m_fieldValue = value;
					break;
				case Mode.Variable:
					IBlackboardRef.SetVariableValue(value, m_behaviour, m_variableGuid, m_variableName, ref m_cachedVariable);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			if (m_variableGuid == default) {
				m_variableName = null;
				return;
			}

			BlackboardVar variable = IBlackboardRef.FindReferencedVariable<T>(m_behaviour, m_variableGuid, m_variableName);
			if (variable != null) {
				m_variableGuid = variable.Guid;
				m_variableName = variable.Name;
			}
		}
		void ISerializationCallbackReceiver.OnAfterDeserialize() { }

		private enum Mode {
			Field,
			Variable,
		}
	}
}
