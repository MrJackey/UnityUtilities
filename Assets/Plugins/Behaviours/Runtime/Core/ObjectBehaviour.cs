using System;
using System.Collections.Generic;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using UnityEngine;

namespace Jackey.Behaviours {
	public abstract class ObjectBehaviour : ScriptableObject {
		[SerializeField] internal Blackboard m_blackboard;

#if UNITY_EDITOR
		[SerializeField] internal EditorData Editor_Data;
#endif

		protected internal BehaviourOwner Owner { get; internal set; }
		public Blackboard Blackboard => m_blackboard;

		internal virtual void Initialize(BehaviourOwner owner) {
			Owner = owner;
		}

		internal abstract void Start();
		internal abstract ExecutionStatus Tick();
		internal abstract void Stop();

#if UNITY_EDITOR
		[Serializable]
		internal class EditorData {
			public List<Group> Groups = new();

			[Serializable]
			public class Group {
				[HideInInspector]
				public string Label;
				[TextArea]
				public string Comments;
				[HideInInspector]
				public Rect Rect;
			}
		}
#endif
	}
}
