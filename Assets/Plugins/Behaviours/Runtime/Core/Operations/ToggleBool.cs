using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Operations {
	[SearchPath("Blackboard/Toggle Bool")]
	public class ToggleBool : BehaviourOperation {
		[SerializeField] private BlackboardOnlyRef<bool> m_variable;

#if UNITY_EDITOR
		public override string Editor_Info => $"Toggle {m_variable.Editor_Info}";
#endif

		protected override void OnExecute() {
			bool value = m_variable.GetValue();
			m_variable.SetValue(!value);
		}
	}
}
