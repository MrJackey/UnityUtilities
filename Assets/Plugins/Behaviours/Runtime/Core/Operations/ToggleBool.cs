using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;

namespace Jackey.Behaviours.Core.Operations {
	[SearchPath("Blackboard/Toggle Bool")]
	public class ToggleBool : Operation {
		public BlackboardOnlyRef<bool> Variable;

#if UNITY_EDITOR
		public override string Editor_Info => $"Toggle {Variable.Editor_Info}";
#endif

		protected override void OnExecute() {
			bool value = Variable.GetValue();
			Variable.SetValue(!value);
		}
	}
}
