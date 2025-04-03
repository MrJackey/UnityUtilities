using System;
using Jackey.Behaviours.Utilities;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public struct BlackboardRef<T> : ISerializationCallbackReceiver {
		[SerializeField] private Mode m_mode;
		[SerializeField] private T m_fieldValue;

		[SerializeField] private ObjectBehaviour m_behaviour;
		[SerializeField] private SerializedGUID m_variableGuid;
		[SerializeField] private string m_variableName;

		[UsedImplicitly] // #if !UNITY_EDITOR
		private BlackboardVar m_cachedVariable;

		public bool IsValue => m_mode is Mode.Field;
		public bool IsVariable => m_mode is Mode.Variable;
		public bool IsEmptyVariable => m_mode is Mode.Variable && m_variableGuid == default;

#if UNITY_EDITOR
		public string Editor_Info {
			get {
				switch (m_mode) {
					case Mode.Field:
						return $"{m_fieldValue?.ToString() ?? string.Empty}";
					case Mode.Variable:
						if (m_variableGuid != default) {
							BlackboardVar variable = GetReferencedVariable();
							return variable != null ? $"<b>({variable.Name})</b>" : $"<color=red><b>({m_variableName})</b></color>";
						}

						return "<b>NONE</b>";
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
#endif

		public T GetValue() {
			return m_mode switch {
				Mode.Field => m_fieldValue,
				Mode.Variable => GetReferenceValue(),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		public void SetValue(T value) {
			switch (m_mode) {
				case Mode.Field:
					m_fieldValue = value;
					break;
				case Mode.Variable:
					SetReferenceValue(value);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private T GetReferenceValue() {
			if (m_variableGuid == default)
				return default;

			BlackboardVar variable = GetReferencedVariable();

			if (variable == null) {
				Debug.LogError("Unable to read from blackboard. Referenced variable does not exist", m_behaviour.Owner);
				return default;
			}

			return variable.GetValue<T>();
		}

		private void SetReferenceValue(T value) {
			BlackboardVar variable = GetReferencedVariable();

			if (variable == null) {
				Debug.LogError("Unable to write to blackboard. Referenced variable does not exist", m_behaviour.Owner);
				return;
			}

			variable.SetValue(value);
		}

		[CanBeNull]
		private BlackboardVar GetReferencedVariable() {
#if UNITY_EDITOR
			return FindReferencedVariable();
#else
			return m_cachedVariable ??= FindReferencedVariable();
#endif
		}

		[CanBeNull]
		private BlackboardVar FindReferencedVariable() {
#if UNITY_EDITOR
			if (!Application.IsPlaying(m_behaviour)) {
				foreach (Blackboard blackboard in Blackboard.Available) {
					if (blackboard == null) continue;

					BlackboardVar availableVariable = blackboard.FindVariableWithGuidOrName(m_variableGuid, m_variableName);
					if (availableVariable != null && availableVariable.IsAssignableTo(typeof(T)))
						return availableVariable;
				}
			}
#endif
			BlackboardVar variable = m_behaviour.m_blackboard.FindVariableWithGuidOrName(m_variableGuid, m_variableName);
			if (variable != null && variable.IsAssignableTo(typeof(T)))
				return variable;

			return null;
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			if (m_variableGuid == default) {
				m_variableName = null;
				return;
			}

			BlackboardVar variable = FindReferencedVariable();
			if (variable != null && variable.IsAssignableTo(typeof(T)))
				m_variableName = variable.Name;
		}
		void ISerializationCallbackReceiver.OnAfterDeserialize() { }

		internal enum Mode {
			Field,
			Variable,
		}
	}
}
