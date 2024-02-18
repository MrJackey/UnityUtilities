using Jackey.Behaviours.Core;

namespace Jackey.Behaviours.BT.Actions.Utility {
	public class RunForever : BehaviourAction {
#if UNITY_EDITOR
		public override string Editor_Info => "Run Forever";
#endif
	}
}
