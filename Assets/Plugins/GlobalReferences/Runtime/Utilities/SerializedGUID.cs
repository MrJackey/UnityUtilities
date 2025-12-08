using System;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jackey.GlobalReferences.Utilities {
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct SerializedGUID : IEquatable<SerializedGUID> {
#if UNITY_EDITOR
		private static byte[] m_editorPropertyBuffer = new byte[16];
#endif

		[FieldOffset(0)]
		[SerializeField] private fixed byte m_bytes[16];

		[FieldOffset(0)]
		private long m_lower;
		[FieldOffset(8)]
		private long m_upper;

		[FieldOffset(0)]
		private int m_i0;
		[FieldOffset(4)]
		private int m_i1;
		[FieldOffset(8)]
		private int m_i2;
		[FieldOffset(12)]
		private int m_i3;

		public SerializedGUID(Guid systemGuid) : this() {
			Span<byte> span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
			bool success = systemGuid.TryWriteBytes(span);
			Debug.Assert(success);

			this = MemoryMarshal.Read<SerializedGUID>(span);
		}

		public SerializedGUID(byte[] bytes) : this() {
			Debug.Assert(bytes.Length == 16);
			this = MemoryMarshal.Read<SerializedGUID>(bytes);
		}

		public static SerializedGUID Generate() {
			return new SerializedGUID(Guid.NewGuid());
		}

		public static bool TryParse(string input, out SerializedGUID result) {
			if (!Guid.TryParse(input, out Guid guid)) {
				result = default;
				return false;
			}

			result = new SerializedGUID(guid);
			return true;
		}

		public bool TryWriteBytes(Span<byte> destination) {
			if (destination.Length != 16)
				return false;

			MemoryMarshal.Write(destination, ref this);
			return true;
		}

#if UNITY_EDITOR
		public static SerializedGUID Editor_GetFromProperty(SerializedProperty guidProperty) {
			SerializedProperty bytesProperty = guidProperty.FindPropertyRelative("m_bytes");
			for (int i = 0; i < 16; i++)
				m_editorPropertyBuffer[i] = (byte)bytesProperty.GetFixedBufferElementAtIndex(i).intValue;

			return new SerializedGUID(m_editorPropertyBuffer);
		}

		public static void Editor_WriteToProperty(SerializedProperty guidProperty, SerializedGUID guid) {
			if (!guid.TryWriteBytes(m_editorPropertyBuffer))
				return;

			SerializedProperty bytesProperty = guidProperty.FindPropertyRelative("m_bytes");
			for (int i = 0; i < 16; i++)
				bytesProperty.GetFixedBufferElementAtIndex(i).intValue = m_editorPropertyBuffer[i];
		}
#endif

		public static bool operator ==(SerializedGUID lhs, SerializedGUID rhs) => lhs.Equals(rhs);
		public static bool operator !=(SerializedGUID lhs, SerializedGUID rhs) => !lhs.Equals(rhs);

		public bool Equals(SerializedGUID other) => m_lower == other.m_lower && m_upper == other.m_upper;
		public override bool Equals(object obj) => obj is SerializedGUID other && Equals(other);

		public override int GetHashCode() {
			return m_i0 ^ m_i1 ^ m_i2 ^ m_i3;
		}

		public override string ToString() {
			return new Guid(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))).ToString();
		}
	}

#if UNITY_EDITOR
	namespace PropertyDrawers {
		[CustomPropertyDrawer(typeof(SerializedGUID))]
		public class SerializedGUIDPropertyDrawer : PropertyDrawer {
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
				return EditorGUIUtility.singleLineHeight;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
				EditorGUI.BeginProperty(position, label, property);
				position = EditorGUI.PrefixLabel(position, label);

				string textValue = SerializedGUID.Editor_GetFromProperty(property).ToString();
				EditorGUI.BeginChangeCheck();
				string fieldValue = EditorGUI.DelayedTextField(position, textValue);
				if (EditorGUI.EndChangeCheck() && SerializedGUID.TryParse(fieldValue, out SerializedGUID result))
					SerializedGUID.Editor_WriteToProperty(property, result);

				EditorGUI.EndProperty();
			}
		}
	}
#endif
}
