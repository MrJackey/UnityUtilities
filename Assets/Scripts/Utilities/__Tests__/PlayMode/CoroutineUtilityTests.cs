using System.Collections;
using Jackey.Utilities.Unity.Coroutines;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jackey.Utilities.__Tests__.PlayMode {
	public class CoroutineUtilityTests {
		[SetUp]
		public void SetUp() { }

		[TearDown]
		public void TearDown() {
			if (CoroutineManager.Exists)
				Object.Destroy(CoroutineManager.Instance.gameObject);
		}

		[UnityTest]
		public IEnumerator CoroutinesCanBeRun() {
			int count = 0;
			IEnumerator Routine() {
					count++;
					yield return null;
					count++;
			}

			CoroutineHandle handle = CoroutineManager.StartNew(Routine());

			Assert.AreEqual(1, count);
			Assert.IsTrue(handle.IsRunning);

			yield return null;

			Assert.AreEqual(2, count);
		}

		[UnityTest]
		public IEnumerator CoroutinesCanBePausedAndResumed() {
			int count = 0;
			IEnumerator Routine() {
				while (true) {
					count++;
					yield return null;
				}
			}

			CoroutineHandle handle = CoroutineManager.StartNew(Routine());

			yield return null;

			handle.Pause();
			Assert.IsTrue(handle.IsPaused);

			yield return null;

			Assert.AreEqual(2, count);

			handle.Resume();
			Assert.IsFalse(handle.IsPaused);

			yield return null;

			Assert.AreEqual(3, count);
		}

		[UnityTest]
		public IEnumerator CoroutinesCanBeStopped() {
			int count = 0;
			IEnumerator Routine() {
				while (true) {
					count++;
					yield return null;
				}
			}

			CoroutineHandle handle = CoroutineManager.StartNew(Routine());

			yield return null;

			Assert.AreEqual(2, count);
			handle.Stop();
			Assert.IsTrue(handle.IsStopped);

			yield return null;

			Assert.AreEqual(2, count);
		}

		[UnityTest]
		public IEnumerator PausedCoroutinesDelaysWaitCompletion() {
			int count = 0;
			IEnumerator Routine() {
				while (true) {
					count++;
					yield return CoroutineHandle.Wait(0.1f);
				}
			}

			CoroutineHandle handle = CoroutineManager.StartNew(Routine());

			Assert.AreEqual(1, count);

			handle.Pause();

			yield return new WaitForSeconds(0.3f);

			Assert.AreEqual(1, count);

			handle.Resume();

			yield return null;

			Assert.AreEqual(1, count);
		}

		[Test]
		public void CoroutineHandlesCanBeCreatedBeforehand() {
			int count = 0;
			IEnumerator Routine() {
				while (true) {
					count++;
					yield return null;
				}
			}

			CoroutineHandle handle = new CoroutineHandle();
			CoroutineManager.StartNew(handle, Routine());

			Assert.AreEqual(1, count);
		}

		[UnityTest]
		public IEnumerator CoroutineHandlesCanBeReused() {
			int count1 = 0;
			IEnumerator Routine1() {
				count1++;
				yield break;
			}

			int count2 = 0;
			IEnumerator Routine2() {
				count2++;
				yield break;
			}

			CoroutineHandle handle = CoroutineManager.StartNew(Routine1());

			yield return null;

			Assert.AreEqual(1, count1);

			CoroutineManager.StartNew(handle, Routine2());

			yield return null;

			Assert.AreEqual(1, count1);
			Assert.AreEqual(1, count2);
		}
	}
}
