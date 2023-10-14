using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jackey.ObjectPool.__Tests__ {
	public class TestComponent : MonoBehaviour, IPoolCallbackReceiver<TestComponent>, IPoolObjectCallbackReceiver {
		public static bool InterfacePoolSetup { get; private set; }

		public bool ManualActive { get; set; }
		public bool InterfaceCreate { get; private set; }
		public bool InterfaceActive { get; private set; }

		void IPoolCallbackReceiver<TestComponent>.PoolCreate(PoolHandle<TestComponent> handle) => InterfacePoolSetup = true;

		void IPoolObjectCallbackReceiver.Create() => InterfaceCreate = true;
		void IPoolObjectCallbackReceiver.Setup() => InterfaceActive = true;
		void IPoolObjectCallbackReceiver.Teardown() => InterfaceActive = false;
	}

	public class TestPOCO : IPoolObjectCallbackReceiver {
		public bool ManualActive { get; set; }
		public bool InterfaceCreate { get; private set; }
		public bool InterfaceActive { get; private set; }

		void IPoolObjectCallbackReceiver.Create() => InterfaceCreate = true;
		void IPoolObjectCallbackReceiver.Setup() => InterfaceActive = true;
		void IPoolObjectCallbackReceiver.Teardown() => InterfaceActive = false;
	}

	public class ObjectPoolTests {
		[SetUp]
		public void SetUp() { }

		[TearDown]
		public void TearDown() {
			ObjectPool.Clear();
		}

		[Test]
		public void GameObjectsCanBeRetrieved() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			Assert.IsNotNull(ObjectPool.Instantiate(original));
		}

		[Test]
		public void APocoCanBeRetrieved() {
			Assert.IsNotNull(ObjectPool.New<TestPOCO>());
		}

		[Test]
		public void ReturnedGameObjectsAreReused() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent @object = ObjectPool.Instantiate(original);

			ObjectPool.Destroy(original, @object);

			TestComponent object2 = ObjectPool.Instantiate(original);
			Assert.AreSame(@object, object2);
		}

		[Test]
		public void ReturnedPocoObjectsAreReused() {
			TestPOCO @object = ObjectPool.New<TestPOCO>();
			ObjectPool.Delete(@object);

			TestPOCO object2 = ObjectPool.New<TestPOCO>();
			Assert.AreSame(@object, object2);
		}

		[Test]
		public void GameObjectHandlesCanRetrieveObjects() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);

			Assert.IsNotNull(ObjectPool.Instantiate(handle));
		}

		[Test]
		public void PocoHandlesCanRetrieveObjects() {
			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();

			Assert.IsNotNull(ObjectPool.New(handle));
		}

		[Test]
		public void GameObjectHandlesCanReturnObjects() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);

			Assert.IsNotNull(ObjectPool.Instantiate(handle));
		}

		[Test]
		public void PocoHandlesCanReturnObjects() {
			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();

			Assert.IsNotNull(ObjectPool.New(handle));
		}

		[Test]
		public void GameObjectsCanBeReturnedWithoutHandle() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent @object = ObjectPool.Instantiate(original);

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);

			ObjectPool.FindAndDestroy(@object);

			Assert.AreEqual(1, handle.FreeCount);
			Assert.AreEqual(0, handle.ActiveCount);
		}

		[Test]
		public void ReturningFreeGameObjectsThrows() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			TestComponent @object = ObjectPool.Instantiate(handle);

			ObjectPool.Destroy(handle, @object);
			Assert.Throws<InvalidOperationException>(() => ObjectPool.Destroy(handle, @object));
		}

		[Test]
		public void ReturningFreePocosThrows() {
			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			TestPOCO instance = ObjectPool.New(handle);

			ObjectPool.Delete(handle, instance);
			Assert.Throws<InvalidOperationException>(() => ObjectPool.Delete(handle, instance));
		}

		[Test]
		public void AutomaticGameObjectReturnsAreReused() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			handle.EnableAutomaticReturns(@object => !@object.ManualActive);

			TestComponent @object = ObjectPool.Instantiate(original);
			@object.ManualActive = false;

			handle.CheckAutomaticReturns();

			TestComponent object2 = ObjectPool.Instantiate(handle);
			Assert.AreSame(@object, object2);
		}

		[Test]
		public void AutomaticPocoReturnsAreReused() {
			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			handle.EnableAutomaticReturns(@object => !@object.ManualActive);

			TestPOCO @object = ObjectPool.New(handle);
			@object.ManualActive = false;

			handle.CheckAutomaticReturns();

			TestPOCO object2 = ObjectPool.New(handle);
			Assert.AreSame(@object, object2);
		}

		[Test]
		public void GameObjectInterfacePoolSetupCallbackIsInvoked() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			ObjectPool.GetHandle(original);

			Assert.IsTrue(TestComponent.InterfacePoolSetup);
		}

		[Test]
		public void GameObjectInterfaceCreateCallbackIsInvoked() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent @object = ObjectPool.Instantiate(original);

			Assert.IsTrue(@object.InterfaceCreate);
		}

		[Test]
		public void PocoInterfaceCreateCallbackIsInvoked() {
			TestPOCO @object = ObjectPool.New<TestPOCO>();

			Assert.IsTrue(@object.InterfaceCreate);
		}

		[Test]
		public void GameObjectInterfaceSetupCallbackIsInvoked() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent @object = ObjectPool.Instantiate(original);

			Assert.IsTrue(@object.InterfaceActive);
		}

		[Test]
		public void PocoInterfaceSetupCallbackIsInvoked() {
			TestPOCO @object = ObjectPool.New<TestPOCO>();

			Assert.IsTrue(@object.InterfaceActive);
		}

		[Test]
		public void GameObjectInterfaceTeardownCallbackIsInvoked() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();

			TestComponent @object = ObjectPool.Instantiate(original);
			ObjectPool.Destroy(original, @object);

			Assert.IsFalse(@object.InterfaceActive);
		}

		[Test]
		public void PocoInterfaceTeardownCallbackIsInvoked() {
			TestPOCO @object = ObjectPool.New<TestPOCO>();
			ObjectPool.Delete(@object);

			Assert.IsFalse(@object.InterfaceActive);
		}

		[Test]
		public void GameObjectPoolCreateCallbackIsInvoked() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent callbackProvidedObject = null;

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			handle.ObjectCreated += @object => callbackProvidedObject = @object;

			TestComponent @object = ObjectPool.Instantiate(handle);

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void PocoPoolCreateCallbackIsInvoked() {
			TestPOCO callbackProvidedObject = null;

			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			handle.ObjectCreated += @object => callbackProvidedObject = @object;

			TestPOCO @object = ObjectPool.New(handle);

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void GameObjectPoolSetupCallbackIsInvoked() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent callbackProvidedObject = null;

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			handle.ObjectSetup += @object => callbackProvidedObject = @object;

			TestComponent @object = ObjectPool.Instantiate(handle);

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void PocoPoolSetupCallbackIsInvoked() {
			TestPOCO callbackProvidedObject = null;

			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			handle.ObjectSetup += @object => callbackProvidedObject = @object;

			TestPOCO @object = ObjectPool.New(handle);

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void GameObjectPoolReturnCallbackIsInvokedWhenReturnedManually() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent callbackProvidedObject = null;

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			handle.ObjectReturned += @object => callbackProvidedObject = @object;

			TestComponent @object = ObjectPool.Instantiate(handle);
			ObjectPool.Destroy(handle, @object);

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void PocoPoolReturnCallbackIsInvokedWhenReturnedManually() {
			TestPOCO callbackProvidedObject = null;

			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			handle.ObjectReturned += @object => callbackProvidedObject = @object;

			TestPOCO @object = ObjectPool.New(handle);
			ObjectPool.Delete(handle, @object);

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void GameObjectPoolReturnCallbackIsInvokedWhenAutomaticallyReturned() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			TestComponent callbackProvidedObject = null;

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			handle.EnableAutomaticReturns(@object => !@object.ManualActive);
			handle.ObjectReturned += @object => callbackProvidedObject = @object;

			TestComponent @object = ObjectPool.Instantiate(handle);
			@object.ManualActive = false;
			handle.CheckAutomaticReturns();

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void PocoPoolReturnCallbackIsInvokedWhenAutomaticallyReturned() {
			TestPOCO callbackProvidedObject = null;

			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			handle.EnableAutomaticReturns(@object => !@object.ManualActive);
			handle.ObjectReturned += @object => callbackProvidedObject = @object;

			TestPOCO @object = ObjectPool.New(handle);
			@object.ManualActive = false;
			handle.CheckAutomaticReturns();

			Assert.AreSame(@object, callbackProvidedObject);
		}

		[Test]
		public void GameObjectAutoReturnedObjectsCanBeImmediatelyReusedViaPoolReturnCallback() {
			TestComponent original = new GameObject().AddComponent<TestComponent>();

			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);
			handle.EnableAutomaticReturns(@object => !@object.ManualActive);

			List<TestComponent> objects = new();

			for (int i = 0; i < 4; i++) {
				TestComponent @object = ObjectPool.Instantiate(original);
				@object.ManualActive = true;

				objects.Add(@object);
			}

			handle.ObjectReturned += _ => ObjectPool.Instantiate(original);

			for (int i = 0; i < 4; i++) {
				objects[i].ManualActive = false;
			}

			handle.CheckAutomaticReturns();

			Assert.AreEqual(4, handle.ActiveCount);
			Assert.AreEqual(0, handle.FreeCount);
		}

		[Test]
		public void PocoAutoReturnedObjectsCanBeImmediatelyReusedViaPoolReturnCallback() {
			PoolHandle<TestPOCO> handle = ObjectPool.GetHandle<TestPOCO>();
			handle.EnableAutomaticReturns(@object => !@object.ManualActive);

			List<TestPOCO> objects = new();

			for (int i = 0; i < 4; i++) {
				TestPOCO @object = ObjectPool.New<TestPOCO>();
				@object.ManualActive = true;

				objects.Add(@object);
			}

			handle.ObjectReturned += _ => ObjectPool.New<TestPOCO>();

			for (int i = 0; i < 4; i++) {
				objects[i].ManualActive = false;
			}

			handle.CheckAutomaticReturns();

			Assert.AreEqual(4, handle.ActiveCount);
			Assert.AreEqual(0, handle.FreeCount);
		}

		// 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20
		// *   *   *   *   *    *     *     *     *     *
		// - + + - + + - + + -  +  +  -  +  +  -  +  +  -  +
		[TestCase(13, 6, 4, 2)]
		[TestCase(10, 5, 3, 2)]
		[TestCase(17, 8, 5, 3)]
		[TestCase(20, 10, 7, 3)]
		public void DestroyedGameObjectsCanBeRemovedFromTheirPool(int initialObjects, int expectedCount, int expectedActive, int expectedFree) {
			TestComponent original = new GameObject().AddComponent<TestComponent>();
			PoolHandle<TestComponent> handle = ObjectPool.GetHandle(original);

			List<TestComponent> objects = new();

			for (int i = 0; i < initialObjects; i++)
				objects.Add(ObjectPool.Instantiate(handle));

			for (int i = 0; i < initialObjects; i += 3)
				ObjectPool.Destroy(handle, objects[i]);

			for (int i = 0; i < initialObjects; i += 2)
				Object.DestroyImmediate(objects[i]);

			ObjectPool.RemoveDestroyedGameObjects();

			Assert.AreEqual(expectedCount, handle.Count);
			Assert.AreEqual(expectedActive, handle.ActiveCount);
			Assert.AreEqual(expectedFree, handle.FreeCount);
		}
	}
}
