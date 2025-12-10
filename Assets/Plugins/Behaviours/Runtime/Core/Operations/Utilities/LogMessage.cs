using System;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Operations.Utilities {
	[SearchPath("Utilities/Log Message")]
	public class LogMessage : BehaviourOperation {
		[SerializeField] private LogType m_type;
		[SerializeField] private BlackboardRef<string> m_message;

#if UNITY_EDITOR
		public override string Editor_Info => $"{m_type}: \"{m_message.Editor_Info}\"";
#endif

		internal override void Execute(BehaviourOwner owner) {
			switch (m_type) {
				case LogType.Log:
					Debug.Log(m_message.GetValue(), owner);
					break;
				case LogType.Warning:
					Debug.LogWarning(m_message.GetValue(), owner);
					break;
				case LogType.Error:
					Debug.LogError(m_message.GetValue(), owner);
					break;
				case LogType.Assert:
					Debug.LogAssertion(m_message.GetValue(), owner);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		protected override void OnExecute() { }

		private enum LogType {
			Log,
			Warning,
			Error,
			Assert,
		}
	}
}
