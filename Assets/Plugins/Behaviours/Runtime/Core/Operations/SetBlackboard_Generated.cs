

using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Variables;
using UnityEngine;

namespace Jackey.Behaviours.Operations {
	[DisplayName("Set Bool")]
	[SearchPath("Blackboard/Set Bool")]
	public class SetBool : Operation {
		[SerializeField] private BlackboardOnlyRef<bool> m_variable;
		[SerializeField] private BlackboardRef<bool> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set String")]
	[SearchPath("Blackboard/Set String")]
	public class SetString : Operation {
		[SerializeField] private BlackboardOnlyRef<string> m_variable;
		[SerializeField] private BlackboardRef<string> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Int")]
	[SearchPath("Blackboard/Set Int")]
	public class SetInt : Operation {
		[SerializeField] private BlackboardOnlyRef<int> m_variable;
		[SerializeField] private BlackboardRef<int> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set uInt")]
	[SearchPath("Blackboard/Set uInt")]
	public class SetuInt : Operation {
		[SerializeField] private BlackboardOnlyRef<uint> m_variable;
		[SerializeField] private BlackboardRef<uint> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Long")]
	[SearchPath("Blackboard/Set Long")]
	public class SetLong : Operation {
		[SerializeField] private BlackboardOnlyRef<long> m_variable;
		[SerializeField] private BlackboardRef<long> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set uLong")]
	[SearchPath("Blackboard/Set uLong")]
	public class SetuLong : Operation {
		[SerializeField] private BlackboardOnlyRef<ulong> m_variable;
		[SerializeField] private BlackboardRef<ulong> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Float")]
	[SearchPath("Blackboard/Set Float")]
	public class SetFloat : Operation {
		[SerializeField] private BlackboardOnlyRef<float> m_variable;
		[SerializeField] private BlackboardRef<float> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Double")]
	[SearchPath("Blackboard/Set Double")]
	public class SetDouble : Operation {
		[SerializeField] private BlackboardOnlyRef<double> m_variable;
		[SerializeField] private BlackboardRef<double> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set GameObject")]
	[SearchPath("Blackboard/Set GameObject")]
	public class SetGameObject : Operation {
		[SerializeField] private BlackboardOnlyRef<GameObject> m_variable;
		[SerializeField] private BlackboardRef<GameObject> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set MonoBehaviour")]
	[SearchPath("Blackboard/Set MonoBehaviour")]
	public class SetMonoBehaviour : Operation {
		[SerializeField] private BlackboardOnlyRef<MonoBehaviour> m_variable;
		[SerializeField] private BlackboardRef<MonoBehaviour> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		protected override void OnExecute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}
}
