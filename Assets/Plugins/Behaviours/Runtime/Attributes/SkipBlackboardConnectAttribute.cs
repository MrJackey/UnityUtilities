using System;
using System.Diagnostics;

namespace Jackey.Behaviours.Attributes {
	[AttributeUsage(AttributeTargets.Field)]
	[Conditional("UNITY_EDITOR")]
	public class SkipBlackboardConnectAttribute : Attribute { }
}
