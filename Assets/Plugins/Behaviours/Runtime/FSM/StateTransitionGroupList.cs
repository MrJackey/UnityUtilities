using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jackey.Behaviours.FSM {
	[Serializable]
	public class StateTransitionGroupList {
		[SerializeField] private List<StateTransitionGroup> m_list = new() { new StateTransitionGroup() };

		public List<StateTransitionGroup> List => m_list;
	}
}
