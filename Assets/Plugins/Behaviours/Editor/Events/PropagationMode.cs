using System;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Events {
	public enum PropagationMode {
		Continue,
		Stop,
		StopImmediately,
	}

	public static class PropagationExtensions {
		public static void Process(this PropagationMode mode, EventBase evt) {
			switch (mode) {
				case PropagationMode.Continue:
					break;
				case PropagationMode.Stop:
					evt.StopPropagation();
					break;
				case PropagationMode.StopImmediately:
					evt.StopImmediatePropagation();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}
	}
}
