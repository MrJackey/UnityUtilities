using Jackey.Behaviours.Attributes;

namespace Jackey.Behaviours.Actions.Utilities {
	[SearchPath("Utilities/Run Forever")]
	public class RunForever : BehaviourAction {
#if UNITY_EDITOR
		public override string Editor_Info => "Run Forever";
#endif
	}
}
