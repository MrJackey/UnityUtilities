using System;
using UnityEngine;

namespace Jackey.Behaviours.Utilities {
	public static class Arithmetic {
		public static float Operate(float lhs, Operation operation, float rhs) {
			return operation switch {
				Operation.Set => rhs,
				Operation.Add => lhs + rhs,
				Operation.Subtract => lhs - rhs,
				Operation.Multiply => lhs * rhs,
				Operation.Divide => lhs / rhs,
				_ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
			};
		}

		public static int Operate(int lhs, Operation operation, int rhs) {
			return operation switch {
				Operation.Set => rhs,
				Operation.Add => lhs + rhs,
				Operation.Subtract => lhs - rhs,
				Operation.Multiply => lhs * rhs,
				Operation.Divide => lhs / rhs,
				_ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
			};
		}

		public static bool Compare(float lhs, Comparison comparison, float rhs) {
			return comparison switch {
				Comparison.LessThan => lhs < rhs,
				Comparison.LessOrEqualTo => lhs <= rhs,
				Comparison.EqualTo => Mathf.Approximately(lhs, rhs),
				Comparison.NotEqualTo => !Mathf.Approximately(lhs, rhs),
				Comparison.GreaterOrEqualTo => lhs >= rhs,
				Comparison.GreaterThan => lhs > rhs,
				_ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
			};
		}

		public static bool Compare(int lhs, Comparison comparison, int rhs) {
			return comparison switch {
				Comparison.LessThan => lhs < rhs,
				Comparison.LessOrEqualTo => lhs <= rhs,
				Comparison.EqualTo => lhs == rhs,
				Comparison.NotEqualTo => lhs != rhs,
				Comparison.GreaterOrEqualTo => lhs >= rhs,
				Comparison.GreaterThan => lhs > rhs,
				_ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
			};
		}

		public static string GetOperationString(Operation operation) {
			return operation switch {
				Operation.Set => "=",
				Operation.Add => "+=",
				Operation.Subtract => "-=",
				Operation.Multiply => "*=",
				Operation.Divide => "/=",
				_ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
			};
		}

		public static string GetComparisonString(Comparison comparison) {
			return comparison switch {
				Comparison.LessThan => "<",
				Comparison.LessOrEqualTo => "<=",
				Comparison.EqualTo => "==",
				Comparison.NotEqualTo => "!=",
				Comparison.GreaterOrEqualTo => ">=",
				Comparison.GreaterThan => ">",
				_ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
			};
		}

		public enum Comparison {
			LessThan,
			LessOrEqualTo,
			EqualTo,
			NotEqualTo,
			GreaterOrEqualTo,
			GreaterThan,
		}

		public enum Operation {
			Set,
			Add,
			Subtract,
			Multiply,
			Divide,
		}
	}
}
