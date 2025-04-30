using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jackey.Behaviours.Core.Blackboard {
	public static class BlackboardConverter {
		private static Dictionary<(Type from, Type to), Delegate> s_conversions = new();

		static BlackboardConverter() {
			s_conversions.Add((typeof(GameObject), typeof(Vector3)), new Func<GameObject, Vector3>(go => go.transform.position));
			s_conversions.Add((typeof(GameObject), typeof(Vector2)), new Func<GameObject, Vector2>(go => go.transform.position));
			s_conversions.Add((typeof(GameObject), typeof(Transform)), new Func<GameObject, Transform>(go => go.transform));

			s_conversions.Add((typeof(Component), typeof(Vector3)), new Func<Component, Vector3>(comp => comp.transform.position));
			s_conversions.Add((typeof(Component), typeof(Vector2)), new Func<Component, Vector2>(comp => comp.transform.position));
			s_conversions.Add((typeof(Component), typeof(Transform)), new Func<Component, Transform>(comp => comp.transform));
			s_conversions.Add((typeof(Component), typeof(GameObject)), new Func<Component, GameObject>(comp => comp.gameObject));

			s_conversions.Add((typeof(float), typeof(int)), new Func<float, int>(f => (int)f));
			s_conversions.Add((typeof(int), typeof(float)), new Func<int, float>(i => (float)i));

			s_conversions.Add((typeof(object), typeof(string)), new Func<object, string>(obj => obj.ToString()));
		}

		public static bool IsConvertible(Type from, Type to) {
			if (to.IsAssignableFrom(from))
				return false;

			foreach ((Type keyFrom, Type keyTo) in s_conversions.Keys) {
				if (to.IsAssignableFrom(keyTo) && keyFrom.IsAssignableFrom(from))
					return true;
			}

			return false;
		}

		public static TResult Convert<TFrom, TResult>(TFrom from) {
			foreach (((Type fromType, Type toType), Delegate conversion) in s_conversions) {
				if (toType.IsAssignableFrom(typeof(TResult)) && fromType.IsAssignableFrom(typeof(TFrom))) {
					return ((Func<TFrom, TResult>)conversion)(from);
				}
			}

			return default;
		}
	}
}
