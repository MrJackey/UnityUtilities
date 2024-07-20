using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public struct BlackboardRef<T> {
		[SerializeField] private Mode m_mode;
		[SerializeField] private T m_fieldValue;

		[SerializeField] private ObjectBehaviour m_behaviour;
		[SerializeField] private string m_variableGuid;

		[UsedImplicitly] // #if !UNITY_EDITOR
		private BlackboardVar m_cachedVariable;

		public bool IsReferencingVariable => m_mode is Mode.Variable && !string.IsNullOrEmpty(m_variableGuid);

#if UNITY_EDITOR
		public string Editor_Info {
			get {
				switch (m_mode) {
					case Mode.Field:
						return $"{m_fieldValue?.ToString() ?? string.Empty}";
					case Mode.Variable:
						BlackboardVar variable = GetReferencedVariable();
						return variable != null ? $"<b>({variable.Name})</b>" : "<b>NONE</b>";
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
			foreach (Blackboard blackboard in Blackboard.Available) {
				if (blackboard == null) continue;

				BlackboardVar variable = blackboard.FindVariable(m_variableGuid);
				if (variable != null)
					return variable;
			}

			return null;
#else
			return m_behaviour.m_blackboard.FindVariable(m_variableGuid);
#endif
		}

		internal enum Mode {
			Field,
			Variable,
		}
	}
}
