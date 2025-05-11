using System;
using Jackey.GlobalReferences.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Jackey.GlobalReferences {
	[DefaultExecutionOrder(-1000000)]
	[DisallowMultipleComponent]
	[ExecuteAlways]
	public sealed class GlobalId : MonoBehaviour {
		[SerializeField] private SerializedGUID m_guid;

		public SerializedGUID GUID => m_guid;
		public GameObject GameObject { get; private set; }

		private void Awake() {
			GameObject = gameObject;

#if UNITY_EDITOR
			// EditMode is handled in OnValidate
			if (!Application.IsPlaying(this) || IsPartOfPrefab())
				return;
#endif

			GlobalReferenceManager.AddInstance(this);
		}

		private void OnDestroy() {
#if UNITY_EDITOR
			if (IsPartOfPrefab())
				return;
#endif

			GlobalReferenceManager.RemoveInstance(this);
		}

#if UNITY_EDITOR
		[NonSerialized] private SerializedGUID m_validationGUID;

		private void OnValidate() {
			if (Application.IsPlaying(this) || IsPartOfPrefab()) return;

			if (m_guid != default && m_guid != m_validationGUID) {
				if (m_validationGUID != default)
					GlobalReferenceManager.MoveEditModeInstance(this, m_validationGUID);
				else
					GlobalReferenceManager.AddInstance(this);
			}
			else if (m_guid == default && m_validationGUID != default) {
				GlobalReferenceManager.RemoveInstance(this, m_validationGUID);
			}

			m_validationGUID = m_guid;
		}

		private bool IsPartOfPrefab() {
			if (PrefabUtility.IsPartOfPrefabAsset(this))
				return true;

			return PrefabStageUtility.GetCurrentPrefabStage() != null;
		}
#endif
	}
}
