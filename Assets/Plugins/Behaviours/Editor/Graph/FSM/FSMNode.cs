using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.Editor.Utilities;
using Jackey.Behaviours.FSM.States;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.FSM {
	public class FSMNode : Node, ITickElement, IConnectionSocketOwner, IConnectionAreaSocket {
		private const float OUT_SOCKET_TANGENT = 2.5f;

		private BehaviourState m_state;
		private BehaviourStatus m_lastRuntimeStateStatus = BehaviourStatus.Inactive;

		private Label m_nameLabel;
		private Image m_icon;
		private Label m_label;

		private List<IConnectionSocket> m_sockets;
		private IConnectionSocket[] m_outSockets;
		private ConnectionSocket m_upSocket;
		private ConnectionSocket m_rightSocket;
		private ConnectionSocket m_downSocket;
		private ConnectionSocket m_leftSocket;

		private Label m_entryLabel;
		private VisualElement m_breakpointElement;

		public BehaviourState State => m_state;
		public IConnectionSocket[] OutSockets => m_outSockets;

		#region IConnectionSocket

		List<IConnectionSocket> IConnectionSocketOwner.Sockets => m_sockets;

		VisualElement IConnectionSocket.Element => this;
		Vector2 IConnectionSocket.Tangent { get; set; }

		int IConnectionSocket.MaxIncomingConnections { get; set; } = -1;
		int IConnectionSocket.MaxOutgoingConnections { get; set; } = -1;
		int IConnectionSocket.IncomingConnections { get; set; }
		int IConnectionSocket.OutgoingConnections { get; set; }


		Vector2 IConnectionAreaSocket.GetOutPoint(Connection connection) {
			Vector2 myPosition = transform.position + this.GetLocalOrigin();

			Vector2 endPosition;
			if (connection.End != null) {
				FSMNode end = connection.End.Element.GetFirstOfType<FSMNode>();
				endPosition = end.transform.position + end.GetLocalOrigin();
			}
			else {
				endPosition = connection.localBound.center; // Estimate its position's direction
			}

			float angle = Vector2.SignedAngle(Vector2.up, myPosition - endPosition);

			return Mathf.Abs(angle) switch {
				> 135f => new Vector2(localBound.width / 2f, localBound.height), // Bottom
				> 45f when angle < 0f => new Vector2(-7f, localBound.height / 2f - 3f), // Left
				> 45f when angle > 0f => new Vector2(localBound.width + 7f, localBound.height / 2f - 3f), // Right
				_ => new Vector2(localBound.width / 2f, -7f), // Top
			};
		}

		Vector2 IConnectionAreaSocket.GetOutTangent(Vector2 point) {
			float angle = Vector2.SignedAngle(Vector2.up, localBound.size / 2f - point);

			return Mathf.Abs(angle) switch {
				> 135f => new Vector2(0f, OUT_SOCKET_TANGENT), // Bottom
				> 45f when angle < 0f => new Vector2(-OUT_SOCKET_TANGENT, 0f), // Left
				> 45f when angle > 0f => new Vector2(OUT_SOCKET_TANGENT, 0f), // Right
				_ => new Vector2(0f, -OUT_SOCKET_TANGENT), // Top
			};
		}

		Vector2 IConnectionAreaSocket.GetInPoint(Connection connection) {
			Vector2 myPosition = transform.position + this.GetLocalOrigin();

			Vector2 startPosition;
			if (connection.Start != null) {
				FSMNode start = connection.Start.Element.GetFirstOfType<FSMNode>();
				startPosition = start.transform.position + start.GetLocalOrigin();
			}
			else {
				startPosition = connection.localBound.center; // Estimate its position's direction
			}

			float angle = Vector2.SignedAngle(Vector2.up, myPosition - startPosition);

			return angle switch {
				> 90f => localBound.size, // BottomRight
				> 0f => new Vector2(localBound.width, 0f), // TopRight
				< -90f => new Vector2(0f, localBound.height), // BottomLeft
				_ => Vector2.zero, // TopLeft
			};
		}

		Vector2 IConnectionAreaSocket.GetInTangent(Vector2 point) => Vector2.zero;

		#endregion

		public FSMNode(BehaviourState state) {
			style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(50f));

			m_state = state;

			hierarchy.Add(m_entryLabel = new Label("Entry") {
				name = "Entry",
			});
			m_entryLabel.SendToBack();

			Add(m_breakpointElement = new VisualElement() { name = "Breakpoint" });
			Add(m_nameLabel = new Label() { name = "StateName" });
			Add(m_icon = new Image() {
				name = "Icon",
				pickingMode = PickingMode.Ignore,
				scaleMode = ScaleMode.ScaleToFit,
			});
			Add(m_label = new Label() {
				pickingMode = PickingMode.Ignore,
			});

			VisualElement rightCenter = new VisualElement() { name = "RightSocketCenter" };
			rightCenter.Add(m_rightSocket = new ConnectionSocket() { name = "RightSocket", IncomingConnections = 0, Tangent = new Vector2(OUT_SOCKET_TANGENT, 0f) });
			VisualElement leftCenter = new VisualElement() { name = "LeftSocketCenter" };
			leftCenter.Add(m_leftSocket = new ConnectionSocket() { name = "LeftSocket", IncomingConnections = 0, Tangent = new Vector2(-OUT_SOCKET_TANGENT, 0f) });

			hierarchy.Add(m_upSocket = new ConnectionSocket() { name = "UpSocket", IncomingConnections = 0, Tangent = new Vector2(0f, -OUT_SOCKET_TANGENT) });
			hierarchy.Add(rightCenter);
			hierarchy.Add(m_downSocket = new ConnectionSocket() { name = "DownSocket", IncomingConnections = 0, Tangent = new Vector2(0f, OUT_SOCKET_TANGENT) });
			hierarchy.Add(leftCenter);

			m_sockets = new List<IConnectionSocket>() { this, m_upSocket, m_rightSocket, m_downSocket, m_leftSocket };
			m_outSockets = new IConnectionSocket[] { m_upSocket, m_rightSocket, m_downSocket, m_leftSocket };

			transform.position = state.Editor_Data.Position;

			SetState(state);
		}

		public void SetState(BehaviourState state) {
			m_state = state;

			Texture icon = GraphIconAttribute.GetTexture(state.GetType());
			m_icon.image = icon;
			m_icon.style.display = icon ? DisplayStyle.Flex : DisplayStyle.None;

			RefreshInfo();
		}

		public void Tick() {
			RefreshInfo();

			if (EditorApplication.isPlaying)
				RuntimeTick();
		}

		private void RefreshInfo() {
			m_nameLabel.text = m_state.Name;
			m_nameLabel.style.display = !string.IsNullOrWhiteSpace(m_state.Name) ? DisplayStyle.Flex : DisplayStyle.None;

			string info = m_state.Editor_Info;

			if (string.IsNullOrEmpty(info)) {
				if (m_icon.image != null) {
					m_label.style.display = DisplayStyle.None;
				}
				else {
					m_label.text = m_state.GetType().Editor_GetDisplayOrTypeName();
					m_label.style.display = DisplayStyle.Flex;
				}
			}
			else {
				m_label.style.display = DisplayStyle.Flex;
				m_label.text = info;
			}

			m_breakpointElement.visible = m_state.Editor_Data.Breakpoint;
		}

		private void RuntimeTick() {
			if (m_state.Status == m_lastRuntimeStateStatus)
				return;

			string previousClass = m_lastRuntimeStateStatus switch {
				BehaviourStatus.Running => "Status-Running",
				BehaviourStatus.Success => "Status-Success",
				BehaviourStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(previousClass))
				contentContainer.RemoveFromClassList(previousClass);

			string nextClass = m_state.Status switch {
				BehaviourStatus.Running => "Status-Running",
				BehaviourStatus.Success => "Status-Success",
				BehaviourStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(nextClass))
				contentContainer.AddToClassList(nextClass);

			m_lastRuntimeStateStatus = m_state.Status;
		}

		public void UpdateEditorData() {
			m_state.Editor_Data.Position = transform.position;
		}

		public void SetEntry(bool isEntry) {
			m_entryLabel.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public void ToggleBreakpoint() {
			bool isBreakpoint = !m_state.Editor_Data.Breakpoint;

			m_state.Editor_Data.Breakpoint = isBreakpoint;
			m_breakpointElement.visible = isBreakpoint;
		}
	}
}
