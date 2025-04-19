using System;

namespace Jackey.Behaviours.Utilities {
	internal interface IJsonWrapper {
		public object BoxedValue { get; }
	}

	[Serializable]
	internal struct JsonWrapper<T> : IJsonWrapper {
		public T Value;

		public object BoxedValue => Value;

		public JsonWrapper(T value) {
			Value = value;
		}
	}
}
