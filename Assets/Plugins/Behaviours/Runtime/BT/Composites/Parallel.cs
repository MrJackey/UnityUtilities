using System;
using Jackey.Behaviours.Actions;
using Jackey.Behaviours.Attributes;
using UnityEngine;

namespace Jackey.Behaviours.BT.Composites {
	[GraphIcon("Parallel")]
	[SearchPath("Composites/Parallel")]
	public class Parallel : Composite {
		[SerializeField] private Policy m_policy;

		private int m_finishedChildren;

#if UNITY_EDITOR
		public override string Editor_Info => UnityEditor.ObjectNames.NicifyVariableName(m_policy.ToString());
		protected internal override int Editor_MaxChildCount => 32;
#endif

		protected override ExecutionStatus OnEnter() {
			m_finishedChildren = 0;

			int childCount = m_children.Count;
			for (int i = 0; i < childCount; i++) {
				BehaviourAction child = m_children[i];
				ExecutionStatus enterStatus = child.EnterSequence();

				if (enterStatus != ExecutionStatus.Running) {
					m_finishedChildren |= 1 << i;

					if (m_policy == Policy.FirstFinish) {
						StopChildren();
						return ExecutionStatus.Success;
					}
				}
			}

			if (m_policy == Policy.AllFinish && m_finishedChildren == (1 << childCount) - 1)
				return ExecutionStatus.Success;

			return ExecutionStatus.Running;
		}

		protected override ExecutionStatus OnChildFinished() {
			// Update the just finished children's bit
			int childCount = m_children.Count;
			for (int i = 0; i < childCount; i++) {
				if (m_children[i].IsFinished)
					m_finishedChildren |= 1 << i;
			}

			// Check if the set policy is fulfilled
			switch (m_policy) {
				case Policy.FirstFinish:
					Debug.Assert(m_finishedChildren > 0);
					StopChildren();

					return ExecutionStatus.Success;
				case Policy.AllFinish:
					if (m_finishedChildren == (1 << childCount) - 1) {
						StopChildren();
						return ExecutionStatus.Success;
					}

					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return ExecutionStatus.Running;
		}

		private void StopChildren() {
			int childCount = m_children.Count;
			for (int i = 0; i < childCount; i++) {
				if ((m_finishedChildren & (1 << i)) != 0) continue;

				m_children[i].Interrupt();
			}
		}

		private enum Policy {
			FirstFinish,
			AllFinish,
		}
	}
}
