using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.FSM.States;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.FSM {
	public class FSMNode : Node, ITickElement, IConnectionSocketOwner, IConnectionSocket {
		private BehaviourState m_state;
		private BehaviourStatus m_lastRuntimeStateStatus = BehaviourStatus.Inactive;

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
		int IConnectionSocket.MaxOutgoingConnections { get; set; } = 0;
		int IConnectionSocket.IncomingConnections { get; set; }
		int IConnectionSocket.OutgoingConnections { get; set; }

		#endregion

		public FSMNode(BehaviourState state) {
			style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(50f));

			m_state = state;

			hierarchy.Add(m_entryLabel = new Label("Entry") {
				name = "Entry",
			});
			m_entryLabel.SendToBack();

			Add(m_breakpointElement = new VisualElement() { name = "Breakpoint" });
			Add(m_icon = new Image() {
				name = "Icon",
				pickingMode = PickingMode.Ignore,
				scaleMode = ScaleMode.ScaleToFit,
			});
			Add(m_label = new Label() {
				pickingMode = PickingMode.Ignore,
			});

			VisualElement rightCenter = new VisualElement() { name = "RightSocketCenter" };
			rightCenter.Add(m_rightSocket = new ConnectionSocket() { name = "RightSocket", Tangent = Vector2.right });
			VisualElement leftCenter = new VisualElement() { name = "LeftSocketCenter" };
			leftCenter.Add(m_leftSocket = new ConnectionSocket() { name = "LeftSocket", Tangent = Vector2.left });

			hierarchy.Add(m_upSocket = new ConnectionSocket() { name = "UpSocket", Tangent = Vector2.down });
			hierarchy.Add(rightCenter);
			hierarchy.Add(m_downSocket = new ConnectionSocket() { name = "DownSocket", Tangent = Vector2.up });
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
			string info = m_state.Editor_Info;

			if (string.IsNullOrEmpty(info)) {
				if (m_icon.image != null) {
					m_label.style.display = DisplayStyle.None;
				}
				else {
					m_label.text = m_state.GetType().GetDisplayOrTypeName();
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

		public void MoveConnectionStartToClosestSocket(Connection connection) {
			Vector2 myPosition = transform.position;
			Vector2 endPosition = connection.End.Element.transform.position;
			float angle = Vector2.SignedAngle(Vector2.up, myPosition - endPosition);

			switch (Mathf.Abs(angle)) {
				case > 135f: // Down
					if (connection.Start == m_downSocket)
						return;

					connection.Start.OutgoingConnections--;
					connection.Start = m_downSocket;

					break;
				case > 45f: // Sides
					if (angle < 0) {
						if (connection.Start == m_leftSocket)
							return;

						connection.Start.OutgoingConnections--;
						connection.Start = m_leftSocket;
					}
					else {
						if (connection.Start == m_rightSocket)
							return;

						connection.Start.OutgoingConnections--;
						connection.Start = m_rightSocket;
					}

					break;
				default: // Up
					if (connection.Start == m_upSocket)
						return;

					connection.Start.OutgoingConnections--;
					connection.Start = m_upSocket;

					break;
			}

			connection.Start.OutgoingConnections++;
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
