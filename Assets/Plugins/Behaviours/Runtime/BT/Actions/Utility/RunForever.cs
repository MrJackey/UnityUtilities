using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Actions.Utility {
	[SearchPath("Utilities/Run Forever")]
	public class RunForever : BehaviourAction {
#if UNITY_EDITOR
		public override string Editor_Info => "Run Forever";
#endif
	}
}
