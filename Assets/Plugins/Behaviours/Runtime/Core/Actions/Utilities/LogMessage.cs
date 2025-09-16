using System;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Log Message")]
	public class LogMessage : BehaviourAction {
		public LogType Type;
		public BlackboardRef<string> Message;

#if UNITY_EDITOR
		public override string Editor_Info => $"{Type}: \"{Message.Editor_Info}\"";
#endif

		protected override ExecutionStatus OnEnter() {
			switch (Type) {
				case LogType.Log:
					Debug.Log(Message.GetValue(), Owner);
					break;
				case LogType.Warning:
					Debug.LogWarning(Message.GetValue(), Owner);
					break;
				case LogType.Error:
					Debug.LogError(Message.GetValue(), Owner);
					break;
				case LogType.Assert:
					Debug.LogAssertion(Message.GetValue(), Owner);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return ExecutionStatus.Success;
		}

		public enum LogType {
			Log,
			Warning,
			Error,
			Assert,
		}
	}
}
