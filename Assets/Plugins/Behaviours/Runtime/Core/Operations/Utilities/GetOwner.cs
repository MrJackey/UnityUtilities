using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;

namespace Jackey.Behaviours.Operations.Utilities {
	[SearchPath("Utilities/Get Owner")]
	public class GetOwner : Operation {
		public BlackboardOnlyRef<BehaviourOwner> Out;

		public override string Editor_Info => $"Set {Out.Editor_Info} to Owner";

		internal override void Execute(BehaviourOwner owner) {
			base.Execute(owner);
			Out.SetValue(owner);
		}

		protected override void OnExecute() { }
	}
}
