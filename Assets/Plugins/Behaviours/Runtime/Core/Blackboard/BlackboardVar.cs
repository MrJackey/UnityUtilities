﻿using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public sealed class BlackboardVar : ISerializationCallbackReceiver {
		[SerializeField] private string m_variableName;
		[SerializeField] private string m_serializedTypeName;
		[SerializeField] private string m_guid;

		[SerializeField] private Object m_unityObjectValue;
		[SerializeReference] private object m_boxedValue;
		// C# primitives are not supported with the SerializeReference attribute
		// https://docs.unity3d.com/ScriptReference/SerializeReference.html
		[SerializeField] private string m_primitiveValue;

		private BlackboardValue m_value;

		internal string Guid => m_guid;

		public BlackboardVar([NotNull] Type type) {
			m_guid = new Guid().ToString();
			m_serializedTypeName = type.AssemblyQualifiedName;
		}

		[CanBeNull]
		internal Type GetSerializedType() => Type.GetType(m_serializedTypeName);

		public string Name {
			get => m_variableName;
			set => m_variableName = value;
		}

		public T GetValue<T>() {
			if (m_value is BlackboardValue<T> val)
				return val.Value;

			return m_value.ConvertTo<T>();
		}

		public void SetValue<T>(T value) {
			if (m_value is BlackboardValue<T> val)
				val.Value = value;
			else if (!m_value.TrySetValue(value))
				Debug.LogError($"Unable to set value of type {typeof(T)} to blackboard variable {m_variableName}");
		}

		internal bool IsAssignableTo(Type type) {
			Type serializedType = GetSerializedType();

			return type == serializedType || BlackboardConverter.IsConvertible(serializedType, type);
		}

		internal abstract class BlackboardValue {
			public abstract TResult ConvertTo<TResult>();
			public abstract bool TrySetValue<T>(T value);
			public abstract void SetValueBoxed(object value);
		}

		internal class BlackboardValue<T> : BlackboardValue {
			public T Value;

			public override bool TrySetValue<TValue>(TValue value) {
				if (value is T val) {
					Value = val;
					return true;
				}

				return false;
			}

			public override void SetValueBoxed(object value) => Value = (T)value;

			[CanBeNull]
			[Pure]
			public override TResult ConvertTo<TResult>() => BlackboardConverter.Convert<T, TResult>(Value);
		}

		#region Serialization

		public void OnBeforeSerialize() { }

		public void OnAfterDeserialize() {
			Type serializedType = Type.GetType(m_serializedTypeName);

			if (serializedType == null)
				return;

			if (serializedType.ContainsGenericParameters) {
				Debug.LogError($"Blackboard variable {m_variableName }has generic parameters. This is not supported! Ensure the type itself does not have generics and that it is not nested in a generic type");
				return;
			}

			Type valueType = typeof(BlackboardValue<>).MakeGenericType(serializedType);
			m_value = (BlackboardValue)Activator.CreateInstance(valueType);

			if (typeof(Object).IsAssignableFrom(serializedType))
				m_value.SetValueBoxed(m_unityObjectValue.GetType() != typeof(Object) ? m_unityObjectValue : null); // Ensure Unity's fake null isn't set as value. If it is, the cast when setting boxed value fails. Unity null / missing are of type Object
			else if (!string.IsNullOrEmpty(m_primitiveValue) && (serializedType == typeof(string) || serializedType.IsPrimitive))
				m_value.SetValueBoxed(Convert.ChangeType(m_primitiveValue, serializedType));
			else if (m_boxedValue != null && serializedType.IsInstanceOfType(m_boxedValue))
				m_value.SetValueBoxed(m_boxedValue);
		}

		#endregion
	}
}
