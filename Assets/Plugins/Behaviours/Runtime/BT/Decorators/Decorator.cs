using Jackey.Behaviours.Actions;
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

		internal override void BT_Initialize(BehaviourTree behaviour, BehaviourAction parent, ref int index) {
			base.BT_Initialize(behaviour, parent, ref index);

			index++;

			if (m_child != null) {
				m_child.Parent = this;
				m_child.BT_Initialize(behaviour, this, ref index);
			}
		}

		protected override ExecutionStatus OnChildFinished() {
			return (ExecutionStatus)m_child.Status;
		}

		internal override void InterruptChildren() {
			if (m_child == null) return;
			if (m_child.Status != BehaviourStatus.Running) return;

			m_child.Interrupt();
		}

		internal override void ResetChildren() {
			if (m_child == null) return;
			if (m_child.Status == BehaviourStatus.Inactive) return;

			m_child.Reset();
		}
	}
}
