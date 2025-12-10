using Jackey.Behaviours.Utilities;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Variables {
	internal interface IBlackboardRef {
		public static string Editor_VariableInfo<T>(ObjectBehaviour behaviour, SerializedGUID guid, string name, ref BlackboardVar cachedVar) {
			if (guid != default) {
				BlackboardVar variable = GetReferencedVariable<T>(behaviour, guid, name, ref cachedVar);
				return variable != null ? $"▤<b>({variable.Name})</b>" : $"▤<color=red><b>({name})</b></color>";
			}

			return "<b>NONE</b>";
		}

		public static T GetVariableValue<T>(ObjectBehaviour behaviour, SerializedGUID guid, string name, ref BlackboardVar cachedVar) {
			if (guid == default)
				return default;

			BlackboardVar variable = GetReferencedVariable<T>(behaviour, guid, name, ref cachedVar);

			if (variable == null) {
				Debug.LogError("Unable to read from blackboard. Referenced variable does not exist", behaviour.Owner);
				return default;
			}

			return variable.GetValue<T>();
		}

		public static void SetVariableValue<T>(T value, ObjectBehaviour behaviour, SerializedGUID guid, string name, ref BlackboardVar cachedVar) {
			BlackboardVar variable = GetReferencedVariable<T>(behaviour, guid, name, ref cachedVar);

			if (variable == null) {
				Debug.LogError("Unable to write to blackboard. Referenced variable does not exist", behaviour.Owner);
				return;
			}

			variable.SetValue(value);
		}

		[CanBeNull]
		private static BlackboardVar GetReferencedVariable<T>(ObjectBehaviour behaviour, SerializedGUID guid, string name, ref BlackboardVar cachedVar) {
#if UNITY_EDITOR
			return FindReferencedVariable<T>(behaviour, guid, name);
#else
			return cachedVar ??= FindReferencedVariable<T>(behaviour, guid, name);
#endif
		}

		[CanBeNull]
		public static BlackboardVar FindReferencedVariable<T>(ObjectBehaviour behaviour, SerializedGUID guid, string name) {
#if UNITY_EDITOR
			if (!Application.IsPlaying(behaviour)) {
				foreach (Blackboard blackboard in Blackboard.Available) {
					if (blackboard == null) continue;

					BlackboardVar availableVariable = blackboard.FindVariableWithGuidOrName(guid, name);
					if (availableVariable != null && availableVariable.IsAssignableTo(typeof(T)))
						return availableVariable;
				}
			}
#endif
			BlackboardVar variable = behaviour.m_blackboard.FindVariableWithGuidOrName(guid, name);
			if (variable != null && variable.IsAssignableTo(typeof(T)))
				return variable;

			return null;
		}
	}
}
