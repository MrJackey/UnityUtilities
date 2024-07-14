using System;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Core {
	[Serializable]
	public abstract class BehaviourAction {
		[CanBeNull]
		internal BehaviourAction Parent { get; set; }
		internal int Index { get; private set; }

		public ActionStatus Status { get; set; } = ActionStatus.Inactive;
		public bool IsFinished => Status is ActionStatus.Success or ActionStatus.Failure;

		protected BehaviourOwner Owner => m_behaviour.Owner;

#if UNITY_EDITOR
		[SerializeField] internal EditorData Editor_Data = new();
#endif
		public virtual string Editor_Info => string.Empty;

		protected BehaviourTree m_behaviour;

		internal virtual void Initialize(BehaviourTree behaviour, [CanBeNull] BehaviourAction parent, ref int index) {
			m_behaviour = behaviour;
			Parent = parent;
			Index = index;
		}

		internal virtual ExecutionStatus Enter() {
#if UNITY_EDITOR
			if (Editor_Data.Breakpoint) {
				Debug.Log($"Behaviour Breakpoint @{m_behaviour.Owner.name}", m_behaviour.Owner);
				UnityEditor.EditorApplication.isPaused = true;
			}
#endif

			ExecutionStatus enterStatus = OnEnter();
			Status = (ActionStatus)enterStatus;

			return enterStatus;
		}

		protected virtual ExecutionStatus OnEnter() => ExecutionStatus.Running;

		internal ExecutionStatus Tick() {
			if (IsFinished) {
				Exit((ActionResult)Status);
				return (ExecutionStatus)Status;
			}

			ExecutionStatus tickStatus = OnTick();
			Status = (ActionStatus)tickStatus;

			if (IsFinished)
				Exit((ActionResult)Status);

			return tickStatus;
		}

		/// <summary>
		/// Actions are ticked once when entered (if not already finished) and whenever a direct child finishes.
		/// To be ticked over multiple frames, use <see cref="EnableTicking"/>
		/// </summary>
		/// <returns></returns>
		protected virtual ExecutionStatus OnTick() => ExecutionStatus.Running;

		internal void Exit(ActionResult result) {
			OnExit(result);
		}

		protected virtual void OnExit(ActionResult result) { }

		protected void EnableTicking() {
			m_behaviour.EnableTicking(this);
		}

		protected void DisableTicking() {
			m_behaviour.DisableTicking(this);
		}

		public void Reset() {
			ResetChildren();
			Status = ActionStatus.Inactive;

			OnReset();
		}
		internal virtual void ResetChildren() { }
		protected virtual void OnReset() { }

#if UNITY_EDITOR
		[Serializable]
		internal class EditorData {
			public Vector2 Position;
			public bool Breakpoint;
		}
#endif
	}

	public abstract class BehaviourAction<T> : BehaviourAction where T : Component {
		[SerializeField] private BlackboardRef<T> m_target;

		protected string TargetInfo => m_target.IsReferencingVariable ? m_target.Editor_Info : "SELF";

		protected T GetTarget() => m_target.GetValue();

		internal override ExecutionStatus Enter() {
			if (m_target.GetValue() == null) {
				if (!Owner.TryGetComponent(out T target)) {
					Debug.LogError($"{nameof(BehaviourAction)} is missing its {typeof(T).Name} target");
					return ExecutionStatus.Failure;
				}

				m_target.SetValue(target);
			}

			return base.Enter();
		}
	}
}
