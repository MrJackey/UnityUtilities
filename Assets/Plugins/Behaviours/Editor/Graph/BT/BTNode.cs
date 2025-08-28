using System.Collections.Generic;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.BT {
	public class BTNode : Node, ITickElement, IConnectionSocketOwner, IConnectionSocket {
		private BehaviourAction m_action;
		private BehaviourStatus m_lastRuntimeActionStatus = BehaviourStatus.Inactive;

		private Image m_icon;
		private Label m_label;
		private ConnectionSocket m_outSocket;
		private List<IConnectionSocket> m_sockets;

		private Label m_entryLabel;
		private VisualElement m_breakpointElement;

		public BehaviourAction Action => m_action;
		public ConnectionSocket OutSocket => m_outSocket;

		#region IConnectionSocket

		VisualElement IConnectionSocket.Element => this;
		Vector2 IConnectionSocket.Tangent { get; set; } = Vector2.down;
		List<IConnectionSocket> IConnectionSocketOwner.Sockets => m_sockets;

		int IConnectionSocket.MaxIncomingConnections { get; set; } = 1;
		int IConnectionSocket.MaxOutgoingConnections { get; set; } = 0;

		int IConnectionSocket.IncomingConnections { get; set; }
		int IConnectionSocket.OutgoingConnections { get; set; }

		#endregion

		public BTNode(BehaviourAction action) {
			style.transformOrigin = new TransformOrigin(Length.Percent(50f), 0f);

			m_action = action;

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

			hierarchy.Add(m_outSocket = new ConnectionSocket() { Tangent = Vector2.up });
			m_sockets = new List<IConnectionSocket> { this, m_outSocket };

			transform.position = action.Editor_Data.Position;

			SetAction(action);
		}

		// TODO: Add ability to replace actions
		public void SetAction(BehaviourAction action) {
			m_action = action;

			Texture icon = GraphIconAttribute.GetTexture(m_action.GetType());
			m_icon.image = icon;
			m_icon.style.display = icon ? DisplayStyle.Flex : DisplayStyle.None;

			RefreshInfo();

			m_outSocket.MaxOutgoingConnections = action.Editor_MaxChildCount;
			m_outSocket.style.display = action is Composite or Decorator
				? DisplayStyle.Flex
				: DisplayStyle.None;

			m_outSocket.MaxIncomingConnections = 0;
		}

		public void Tick() {
			RefreshInfo();

			if (EditorApplication.isPlaying)
				RuntimeTick();
		}

		private void RefreshInfo() {
			string info = m_action.Editor_Info;

			if (string.IsNullOrEmpty(info)) {
				if (m_icon.image != null) {
					m_label.style.display = DisplayStyle.None;
				}
				else {
					m_label.text = m_action.GetType().Editor_GetDisplayOrTypeName();
					m_label.style.display = DisplayStyle.Flex;
				}
			}
			else {
				m_label.style.display = DisplayStyle.Flex;
				m_label.text = info;
			}

			m_breakpointElement.visible = m_action.Editor_Data.Breakpoint;
		}

		private void RuntimeTick() {
			if (m_action.Status == m_lastRuntimeActionStatus)
				return;

			string previousClass = m_lastRuntimeActionStatus switch {
				BehaviourStatus.Running => "Status-Running",
				BehaviourStatus.Success => "Status-Success",
				BehaviourStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(previousClass))
				contentContainer.RemoveFromClassList(previousClass);

			string nextClass = m_action.Status switch {
				BehaviourStatus.Running => "Status-Running",
				BehaviourStatus.Success => "Status-Success",
				BehaviourStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(nextClass))
				contentContainer.AddToClassList(nextClass);

			m_lastRuntimeActionStatus = m_action.Status;
		}

		public void UpdateEditorData() {
			m_action.Editor_Data.Position = transform.position;
		}

		public void SetEntry(bool isEntry) {
			m_entryLabel.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public void ToggleBreakpoint() {
			bool isBreakpoint = !m_action.Editor_Data.Breakpoint;

			m_action.Editor_Data.Breakpoint = isBreakpoint;
			m_breakpointElement.visible = isBreakpoint;
		}
	}
}
