using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.FSM.States;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.FSM {
	public class FSMNode : Node, ITickElement, IConnectionSocketOwner, IConnectionAreaSocket {
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
		int IConnectionSocket.MaxOutgoingConnections { get; set; } = 0;
		int IConnectionSocket.IncomingConnections { get; set; }
		int IConnectionSocket.OutgoingConnections { get; set; }

		Vector2 IConnectionAreaSocket.GetPoint(Connection connection) {
			Vector2 myPosition = transform.position;
			Vector2 startPosition = connection.Start != null
				? connection.Start.Element.GetFirstOfType<FSMNode>().transform.position
				: connection.localBound.center; // Estimate its position's direction
			float angle = Vector2.SignedAngle(Vector2.up, myPosition - startPosition);

			return angle switch {
				> 90f => new Vector2(localBound.width, localBound.height), // BottomRight
				> 0f => new Vector2(localBound.width, 0f), // TopRight
				< -90f => new Vector2(0f, localBound.height), // BottomLeft
				_ => Vector2.zero, // TopLeft
			};
		}

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
			rightCenter.Add(m_rightSocket = new ConnectionSocket() { name = "RightSocket", Tangent = new Vector2(2.5f, 0f) });
			VisualElement leftCenter = new VisualElement() { name = "LeftSocketCenter" };
			leftCenter.Add(m_leftSocket = new ConnectionSocket() { name = "LeftSocket", Tangent = new Vector2(-2.5f, 0f) });

			hierarchy.Add(m_upSocket = new ConnectionSocket() { name = "UpSocket", Tangent = new Vector2(0f, -2.5f) });
			hierarchy.Add(rightCenter);
			hierarchy.Add(m_downSocket = new ConnectionSocket() { name = "DownSocket", Tangent = new Vector2(0f, 2.5f) });
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

			IConnectionSocket closest = Mathf.Abs(angle) switch {
				> 135f => m_downSocket,
				> 45f when angle < 0 => m_leftSocket,
				> 45f when angle > 0 => m_rightSocket,
				_ => m_upSocket,
			};

			if (closest == connection.Start)
				return;

			connection.Start.OutgoingConnections--;
			connection.Start = closest;
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
