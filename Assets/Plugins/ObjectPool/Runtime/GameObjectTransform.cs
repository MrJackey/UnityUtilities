using UnityEngine;

namespace Jackey.ObjectPool {
	internal struct GameObjectTransform {
		public Vector3 Position;
		public Quaternion Rotation;
		public Transform Parent;
		public bool WorldSpace;

		public bool InheritPosition;

		public T Create<T>(T original) where T : Object {
			if (InheritPosition)
				return Object.Instantiate(original, Parent, WorldSpace);

			return Object.Instantiate(original, Position, Rotation, Parent);
		}

		public void Apply<T>(T @object) where T : Object {
			Transform objectTransform = @object switch {
				GameObject gameObject => gameObject.transform,
				Component component => component.transform,
				_ => null,
			};

			Debug.Assert(objectTransform, "[ObjectPool] Tried to transform an object without a transform");

			objectTransform.SetParent(Parent);

			if (WorldSpace) {
				objectTransform.SetPositionAndRotation(Position, Rotation);
			}
			else {
				objectTransform.localPosition = Position;
				objectTransform.localRotation = Rotation;
			}
		}
	}
}
