using System;
using Jackey.GlobalReferences.Utilities;
using UnityEngine;

namespace Jackey.GlobalReferences {
	[Serializable]
	public struct GlobalRef {
		[SerializeField] private SerializedGUID m_guid;

		public SerializedGUID GUID => m_guid;

		#region Resolve

		public GameObject Resolve() => GlobalReferenceManager.Resolve(m_guid);
		public T Resolve<T>() => GlobalReferenceManager.Resolve<T>(m_guid);
		public T ResolveInParent<T>() => GlobalReferenceManager.ResolveInParent<T>(m_guid);
		public T ResolveInChildren<T>() => GlobalReferenceManager.ResolveInChildren<T>(m_guid);

		public bool TryResolve(out GameObject gameObject) => GlobalReferenceManager.TryResolve(m_guid, out gameObject);
		public bool TryResolve<T>(out T component) => GlobalReferenceManager.TryResolve(m_guid, out component);
		public bool TryResolveInParent<T>(out T component) => GlobalReferenceManager.TryResolveInParent<T>(m_guid, out component);
		public bool TryResolveInChildren<T>(out T component) => GlobalReferenceManager.TryResolveInChildren<T>(m_guid, out component);

		#endregion
	}
}
