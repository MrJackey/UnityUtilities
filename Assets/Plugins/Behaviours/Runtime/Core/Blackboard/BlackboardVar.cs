using System;
using Jackey.Behaviours.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.Behaviours.Core.Blackboard {
	[Serializable]
	public sealed class BlackboardVar : ISerializationCallbackReceiver {
		[SerializeField] private string m_variableName;
		[SerializeField] private string m_serializedTypeName;
		[SerializeField] private SerializedGUID m_guid;
#if UNITY_EDITOR
		// For some reason the SerializedGUID field is not properly written to when redoing creation of a BlackboardVar.
		// My guess is due to it being a fixed buffer.
		// The backup solution is this field which always contain the same guid to recreate the efficient struct in case it's lost.
		// This field never be used aside from that.
		[SerializeField] private string m_guidString;
#endif

		[SerializeField] private Object m_unityObjectValue;
		[SerializeReference] private object m_boxedValue;
		// C# primitives are not supported with the SerializeReference attribute
		// https://docs.unity3d.com/ScriptReference/SerializeReference.html
		[SerializeField] private string m_primitiveValue;

		private BlackboardValue m_value;

		internal SerializedGUID Guid => m_guid;

		public BlackboardVar([NotNull] Type type) {
			m_guid = SerializedGUID.Generate();
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

			if (m_value.TryGetValue(out T result))
				return result;

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

			return serializedType.IsAssignableFrom(type) || BlackboardConverter.IsConvertible(serializedType, type);
		}

		internal abstract class BlackboardValue {
			public abstract bool TryGetValue<T>(out T result);

			public abstract bool TrySetValue<T>(T value);
			public abstract void SetValueBoxed(object value);

			public abstract TResult ConvertTo<TResult>();
		}

		internal class BlackboardValue<T> : BlackboardValue {
			public T Value;

			public override bool TryGetValue<TResult>(out TResult result) {
				if (Value is TResult val) {
					result = val;
					return true;
				}

				result = default;
				return false;
			}

			public override bool TrySetValue<TValue>(TValue value) {
				if (value == null) {
					Value = default;
					return true;
				}

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

		void ISerializationCallbackReceiver.OnBeforeSerialize() { }

		void ISerializationCallbackReceiver.OnAfterDeserialize() {
#if UNITY_EDITOR
			// Recreate guid in case it's lost
			if (m_guid == default && !string.IsNullOrEmpty(m_guidString))
				SerializedGUID.TryParse(m_guidString, out m_guid);
#endif

			Type serializedType = Type.GetType(m_serializedTypeName);
			if (serializedType == null)
				return;

			if (serializedType.ContainsGenericParameters) {
				Debug.LogError($"Blackboard variable {m_variableName} has generic parameters. This is not supported! Ensure the type \"{serializedType.FullName}\" does not have generics and that it is not nested in a generic type");
				return;
			}

			Type valueType = typeof(BlackboardValue<>).MakeGenericType(serializedType);
			m_value = (BlackboardValue)Activator.CreateInstance(valueType);

			if (typeof(Object).IsAssignableFrom(serializedType)) {
				// Ensure Unity's fake null isn't set as value. If it is, the cast when setting boxed value fails. Unity null / missing are of type Object
				m_value.SetValueBoxed(m_unityObjectValue.GetType() != typeof(Object) ? m_unityObjectValue : null);
			}
			else if (!string.IsNullOrEmpty(m_primitiveValue)) {
				if (serializedType == typeof(string) || serializedType.IsPrimitive) {
					m_value.SetValueBoxed(Convert.ChangeType(m_primitiveValue, serializedType));
				}
				else if (serializedType.IsEnum) {
					m_value.SetValueBoxed(Enum.Parse(serializedType, m_primitiveValue));
				}
				else {
					object jsonValue = JsonUtility.FromJson(m_primitiveValue, typeof(JsonWrapper<>).MakeGenericType(serializedType));
					m_value.SetValueBoxed(((IJsonWrapper)jsonValue).BoxedValue);
				}
			}
			else if (m_boxedValue != null && serializedType.IsInstanceOfType(m_boxedValue)) {
				m_value.SetValueBoxed(m_boxedValue);
			}
		}

		#endregion
	}
}
