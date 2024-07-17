using System.Collections.Generic;
using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.BT.Composites {
	public abstract class Composite : BehaviourAction {
		[HideInInspector]
		[SerializeReference] protected List<BehaviourAction> m_children = new();

#if UNITY_EDITOR
		protected internal override int Editor_MaxChildCount => -1;
#endif

		internal List<BehaviourAction> Children => m_children;

		// TODO: Remove
		public override string Editor_Info {
			get {
				if (m_children.Count == 0)
					return "No Children";

				string ret = string.Empty;

				for (int i = 0; i < m_children.Count; i++) {
					if (i > 0)
						ret += '\n';

					ret += $"[{i}] {m_children[i].GetType().Name}";
				}

				return ret;
			}
		}

		internal override void Initialize(BehaviourTree behaviour, BehaviourAction parent, ref int index) {
			base.Initialize(behaviour, parent, ref index);

			int childCount = m_children.Count;
			for (int i = 0; i < childCount; i++) {
				index++;
				m_children[i].Initialize(behaviour, this, ref index);
			}
		}

		internal override void InterruptChildren() {
			int childCount = m_children.Count;
			for (int i = 0; i < childCount; i++) {
				if (m_children[i].Status != ActionStatus.Running) continue;

				m_children[i].Interrupt();
			}
		}

		internal override void ResetChildren() {
			int childCount = m_children.Count;
			for (int i = 0; i < childCount; i++) {
				if (m_children[i].Status == ActionStatus.Inactive) continue;

				m_children[i].Reset();
			}
		}

#if UNITY_EDITOR
		internal void Editor_OrderChildren() {
			// Inline insertion sort
			for (int i = 1; i < m_children.Count; i++) {
				for (int j = i; j > 0; j--) {
					BehaviourAction lhs = m_children[j - 1];
					BehaviourAction rhs = m_children[j];

					if (lhs.Editor_Data.Position.x <= rhs.Editor_Data.Position.x)
						break;

					m_children[j - 1] = rhs;
					m_children[j] = lhs;
				}
			}
		}
#endif
	}
}
