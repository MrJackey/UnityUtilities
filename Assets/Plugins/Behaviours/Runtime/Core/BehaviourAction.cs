using System;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.Core.Blackboard;
using JetBrains.Annotations;
using UnityEngine;

namespace Jackey.Behaviours.Core {
	[Serializable]
	public abstract class BehaviourAction {
		protected BehaviourTree m_runtimeBehaviour;

		[CanBeNull]
		internal BehaviourAction Parent { get; set; }
		internal int Index { get; private set; }

		public ActionStatus Status { get; set; } = ActionStatus.Inactive;
		public bool IsFinished => Status is ActionStatus.Success or ActionStatus.Failure;

		protected BehaviourOwner Owner => m_runtimeBehaviour.Owner;

#if UNITY_EDITOR
		[SerializeField] internal EditorData Editor_Data = new();
#endif
		public virtual string Editor_Info => string.Empty;
		protected internal virtual int Editor_MaxChildCount => 0;

		internal virtual void Initialize(BehaviourTree behaviour, [CanBeNull] BehaviourAction parent, ref int index) {
			m_runtimeBehaviour = behaviour;
			Parent = parent;
			Index = index;
		}

		internal virtual ExecutionStatus Enter() {
#if UNITY_EDITOR
			if (Editor_Data.Breakpoint) {
				Debug.Log($"Behaviour Breakpoint @{Owner.name}", Owner);
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
		/// Invoked whenever the behaviour is ticked if <see cref="EnableTicking"/> has been called.
		/// Use <see cref="DisableTicking"/> to stop ticking this action
		/// </summary>
		protected virtual ExecutionStatus OnTick() => ExecutionStatus.Running;

		internal ExecutionStatus OnTraversal() {
			if (IsFinished)
				return (ExecutionStatus)Status;

			ExecutionStatus continueStatus = OnChildFinished();
			Status = (ActionStatus)continueStatus;

			return continueStatus;
		}
		protected virtual ExecutionStatus OnChildFinished() => ExecutionStatus.Success;

		/// <summary>
		/// Interrupt this action and all of its children
		/// </summary>
		public void Interrupt() {
			InterruptChildren();
			Status = ActionStatus.Failure;

			OnInterrupt();
			Exit();
		}
		internal virtual void InterruptChildren() { }

		/// <summary>
		/// Actions are interrupted when any of their ancestors interrupts their running children.
		/// This can be a composite like <see cref="Jackey.Behaviours.BT.Composites.Parallel"/> or a decorator like <see cref="Jackey.Behaviours.BT.Decorators.Interruptor"/>.
		/// <br/>
		/// <see cref="OnExit"/> is invoked directly afterwards
		/// </summary>
		protected virtual void OnInterrupt() { }

		internal void Result(ActionResult result) {
			OnResult(result);
			Exit();
		}

		/// <summary>
		/// This is invoked when an action finishes from any of OnEnter/OnTick/OnChildFinished with a result.
		/// <see cref="OnExit"/> is invoked directly afterwards
		/// </summary>
		protected virtual void OnResult(ActionResult result) { }

		internal void Exit() {
			OnExit();
		}

		/// <summary>
		/// Invoked whenever the action is exited, either by finishing its execution or by being interrupted.
		/// See <see cref="OnInterrupt"/> and <see cref="OnResult"/> for callbacks to these events
		/// </summary>
		protected virtual void OnExit() { }

		/// <summary>
		/// Enable ticking of this action whenever it's behaviour is ticked.
		/// </summary>
		/// <remarks>
		/// If the behaviour is starting, the first tick will occur after the first behaviour tick.<br/>
		/// If the behaviour is being ticked, the first tick will occur the next behaviour tick
		/// </remarks>
		public void EnableTicking() {
			m_runtimeBehaviour.EnableTicking(this);
		}

		/// <summary>
		/// Disable ticking of this action.
		/// Does nothing if ticking is not already enabled
		/// </summary>
		public void DisableTicking() {
			m_runtimeBehaviour.DisableTicking(this);
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
			}
			return enterStatus;
		}

		internal ExecutionStatus TickSequence() {
			ExecutionStatus tickStatus = Tick();

			if (IsFinished) {
				Result((ActionResult)tickStatus);
			}

			return tickStatus;
		}

		internal ExecutionStatus TraversalSequence() {
			ExecutionStatus traversalStatus = OnTraversal();

			if (IsFinished) {
				Result((ActionResult)traversalStatus);
			}

			return traversalStatus;
		}

#if UNITY_EDITOR
		[Serializable]
		internal class EditorData {
			public Vector2 Position;
			public bool Breakpoint;
		}
#endif
	}

	public abstract class BehaviourAction<T> : BehaviourAction {
		[SerializeField] private BlackboardRef<T> m_target;

		protected string TargetInfo => m_target.IsVariable ? m_target.Editor_Info : "SELF";

		protected T GetTarget() => m_target.GetValue();

		internal override ExecutionStatus Enter() {
			if (!Owner.SetTargetIfNeeded(ref m_target)) {
				Debug.LogError($"{nameof(BehaviourAction)} is missing its {typeof(T).Name} target");
				return ExecutionStatus.Failure;
			}

			return base.Enter();
		}
	}
}
