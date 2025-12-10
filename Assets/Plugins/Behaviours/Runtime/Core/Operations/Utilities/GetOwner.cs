using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Operations.Utilities {
	[SearchPath("Utilities/Get Owner")]
	public class GetOwner : BehaviourOperation {
		[SerializeField] private BlackboardOnlyRef<BehaviourOwner> m_out;

	#if UNITY_EDITOR
		public override string Editor_Info => $"Set {m_out.Editor_Info} to Owner";
#endif

		internal override void Execute(BehaviourOwner owner) {
			m_out.SetValue(owner);
		}

		protected override void OnExecute() { }
	}
}
