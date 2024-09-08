

using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core.Blackboard;
using Jackey.Behaviours.Core.Operations;
using UnityEngine;

namespace Jackey.Behaviours.BT.Operations {
	[DisplayName("Set Bool")]
	[SearchPath("Blackboard/Set Bool")]
	public class SetBool : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<bool> m_variable;
		[SerializeField] private BlackboardRef<bool> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set String")]
	[SearchPath("Blackboard/Set String")]
	public class SetString : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<string> m_variable;
		[SerializeField] private BlackboardRef<string> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Int")]
	[SearchPath("Blackboard/Set Int")]
	public class SetInt : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<int> m_variable;
		[SerializeField] private BlackboardRef<int> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set uInt")]
	[SearchPath("Blackboard/Set uInt")]
	public class SetuInt : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<uint> m_variable;
		[SerializeField] private BlackboardRef<uint> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Long")]
	[SearchPath("Blackboard/Set Long")]
	public class SetLong : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<long> m_variable;
		[SerializeField] private BlackboardRef<long> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set uLong")]
	[SearchPath("Blackboard/Set uLong")]
	public class SetuLong : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<ulong> m_variable;
		[SerializeField] private BlackboardRef<ulong> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Float")]
	[SearchPath("Blackboard/Set Float")]
	public class SetFloat : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<float> m_variable;
		[SerializeField] private BlackboardRef<float> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set Double")]
	[SearchPath("Blackboard/Set Double")]
	public class SetDouble : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<double> m_variable;
		[SerializeField] private BlackboardRef<double> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set GameObject")]
	[SearchPath("Blackboard/Set GameObject")]
	public class SetGameObject : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<GameObject> m_variable;
		[SerializeField] private BlackboardRef<GameObject> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}

	[DisplayName("Set MonoBehaviour")]
	[SearchPath("Blackboard/Set MonoBehaviour")]
	public class SetMonoBehaviour : Operation {
		[BlackboardOnly]
		[SerializeField] private BlackboardRef<MonoBehaviour> m_variable;
		[SerializeField] private BlackboardRef<MonoBehaviour> m_value;

		public override string Editor_Info => $"Set {m_variable.Editor_Info} to {m_value.Editor_Info}";

		public override void Execute() {
			m_variable.SetValue(m_value.GetValue());
		}
	}
}
