using System;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Core;
using UnityEngine;

namespace Jackey.Behaviours.BT.Actions {
	[SearchPath("Utilities/Tick Component")]
	public class TickComponent : BehaviourAction<MonoBehaviour> {
		protected override ExecutionStatus OnEnter() => GetInterfaceTarget().OnEnter(this);
		protected override ExecutionStatus OnTick() => GetInterfaceTarget().OnTick(this);

		protected override void OnInterrupt() => GetInterfaceTarget().OnInterrupt(this);
		protected override void OnResult(ActionResult result) => GetInterfaceTarget().OnResult(this, result);
		protected override void OnExit() => GetInterfaceTarget().OnExit(this);

		private IComponentAction GetInterfaceTarget() {
			MonoBehaviour component = GetTarget();

			if (component is not IComponentAction target)
				throw new InvalidOperationException($"Component \"{component}\" must implement {nameof(IComponentAction)} to be able to be ticked from a behaviour");

			return target;
		}
	}
}
