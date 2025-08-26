using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jackey.Behaviours.FSM {
	[Serializable]
	public class TransitionList {
		[SerializeField] private List<StateTransition> m_list = new();

		internal List<StateTransition> List => m_list;
	}
}
