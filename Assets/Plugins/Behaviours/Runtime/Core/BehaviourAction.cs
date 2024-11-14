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
		protected internal virtual int Editor_MaxChildCount => 0;

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
			if (IsFinished)
				return (ExecutionStatus)Status;

			ExecutionStatus tickStatus = OnTick();
			Status = (ActionStatus)tickStatus;

			return tickStatus;
		}

		/// <summary>
		/// Actions are ticked once when entered (if not already finished) and whenever a direct child finishes.
		/// To be ticked over multiple frames, use <see cref="EnableTicking"/>
		/// </summary>
		/// <returns></returns>
		protected virtual ExecutionStatus OnTick() => ExecutionStatus.Running;

		internal void Interrupt() {
			InterruptChildren();
			Status = ActionStatus.Inactive;

			OnInterrupt();
			Exit();
		}
		internal virtual void InterruptChildren() { }

		/// <summary>
		/// Actions are interrupted when an action with higher earlier in the tree finishes with still running actions.
		/// <see cref="OnExit"/> is invoked directly afterwards
		/// </summary>
		protected virtual void OnInterrupt() { }

		internal void Result(ActionResult result) {
			OnResult(result);
			Exit();
		}

		/// <summary>
		/// This is invoked when an action is finishes OnEnter or OnTick with a result.
		/// <see cref="OnExit"/> is invoked directly afterwards
		/// </summary>
		/// <param name="result">The result of the action execution</param>
		protected virtual void OnResult(ActionResult result) { }

		internal void Exit() {
			OnExit();
		}

		/// <summary>
		/// Invoked whenever the action is exited, either by finishing its execution or by being interrupted.
		/// See <see cref="OnInterrupt"/> and <see cref="OnResult"/> for callbacks to these events
		/// </summary>
		protected virtual void OnExit() { }

		public void EnableTicking() {
			m_behaviour.EnableTicking(this);
		}

		public void DisableTicking() {
			m_behaviour.DisableTicking(this);
		}

		public void Reset() {
			ResetChildren();
			Status = ActionStatus.Inactive;

			OnReset();
		}
		internal virtual void ResetChildren() { }
		protected virtual void OnReset() { }

		public ExecutionStatus EnterSequence() {
			ExecutionStatus enterStatus = Enter();

			if (IsFinished) {
				Result((ActionResult)enterStatus);
				return enterStatus;
			}

			ExecutionStatus tickStatus = Tick();

			if (IsFinished) {
				Result((ActionResult)tickStatus);
			}

			return tickStatus;
		}

		public ExecutionStatus TickSequence() {
			ExecutionStatus tickStatus = Tick();

			if (IsFinished) {
				Result((ActionResult)tickStatus);
			}

			return tickStatus;
		}

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
