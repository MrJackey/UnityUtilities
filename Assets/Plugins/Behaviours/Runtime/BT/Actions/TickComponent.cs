using System;
using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.BT.Actions {
	[SearchPath("Utilities/Tick Component")]
	public class TickComponent : BehaviourAction<MonoBehaviour> {
		protected override ExecutionStatus OnEnter() => GetInterfaceTarget().OnEnter();
		protected override ExecutionStatus OnTick() => GetInterfaceTarget().OnTick();

		protected override void OnInterrupt() => GetInterfaceTarget().OnInterrupt();
		protected override void OnResult(ActionResult result) => GetInterfaceTarget().OnResult(result);
		protected override void OnExit() => GetInterfaceTarget().OnExit();

		private IComponentAction GetInterfaceTarget() {
			MonoBehaviour component = GetTarget();

			if (component is not IComponentAction target)
				throw new InvalidOperationException($"Component \"{component}\" must implement {nameof(IComponentAction)} to be able to be ticked from a behaviour");

			return target;
		}
	}
}
