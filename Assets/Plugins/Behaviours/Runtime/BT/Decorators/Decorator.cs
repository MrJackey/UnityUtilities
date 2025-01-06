using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.BT.Decorators {
	public abstract class Decorator : BehaviourAction {
		[HideInInspector]
		[SerializeReference] protected BehaviourAction m_child;

#if UNITY_EDITOR
		protected internal override int Editor_MaxChildCount => 1;
#endif

		internal BehaviourAction Child {
			get => m_child;
			set => m_child = value;
		}

		internal override void Initialize(BehaviourTree behaviour, BehaviourAction parent, ref int index) {
			base.Initialize(behaviour, parent, ref index);

			index++;

			if (m_child != null) {
				m_child.Parent = this;
				m_child.Initialize(behaviour, this, ref index);
			}
		}

		internal override void InterruptChildren() {
			if (m_child == null) return;
			if (m_child.Status == ActionStatus.Running) return;

			m_child.Interrupt();
		}

		internal override void ResetChildren() {
			if (m_child == null) return;
			if (m_child.Status == ActionStatus.Inactive) return;

			m_child.Reset();
		}
	}
}
