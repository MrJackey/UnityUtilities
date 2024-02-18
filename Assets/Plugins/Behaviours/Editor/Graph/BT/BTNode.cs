using System.Collections.Generic;
using System.Reflection;
using Jackey.Behaviours.Attributes;
using Jackey.Behaviours.BT;
using Jackey.Behaviours.BT.Composites;
using Jackey.Behaviours.BT.Decorators;
using Jackey.Behaviours.Core;
using Jackey.Behaviours.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.BT {
	public class BTNode : Node, ITickElement, IConnectionSocketOwner, IConnectionSocket {
		private static Dictionary<string, Texture> s_iconCache = new();

		private BehaviourAction m_action;
		private ActionStatus m_actionStatus = ActionStatus.Inactive;

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
		Vector2 IConnectionSocket.Tangent => Vector2.down;
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

			hierarchy.Add(m_outSocket = new ConnectionSocket());
			m_sockets = new List<IConnectionSocket> { this, m_outSocket };

			transform.position = action.Editor_Data.Position;

			SetAction(action);
		}

		// TODO: Add ability to replace actions
		public void SetAction(BehaviourAction action) {
			m_action = action;

			Texture icon = GetActionIcon(m_action);
			m_icon.image = icon;
			m_icon.style.display = icon ? DisplayStyle.Flex : DisplayStyle.None;

			RefreshInfo();

			switch (action) {
				case Composite:
					m_outSocket.MaxOutgoingConnections = -1;
					m_outSocket.style.display = DisplayStyle.Flex;
					break;
				case Decorator:
					m_outSocket.MaxOutgoingConnections = 1;
					m_outSocket.style.display = DisplayStyle.Flex;
					break;
				default:
					m_outSocket.style.display = DisplayStyle.None;
					break;
			}

			m_outSocket.MaxIncomingConnections = 0;
		}

		public void Tick() {
			RefreshInfo();
			UpdateEditorData();

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
					m_label.text = ObjectNames.NicifyVariableName(m_action.GetType().Name);
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
			if (m_action.Status == m_actionStatus)
				return;

			string previousClass = m_actionStatus switch {
				ActionStatus.Running => "Status-Running",
				ActionStatus.Success => "Status-Success",
				ActionStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(previousClass))
				contentContainer.RemoveFromClassList(previousClass);

			string nextClass = m_action.Status switch {
				ActionStatus.Running => "Status-Running",
				ActionStatus.Success => "Status-Success",
				ActionStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(nextClass))
				contentContainer.EnsureClass(nextClass);

			m_actionStatus = m_action.Status;
		}

		private void UpdateEditorData() {
			m_action.Editor_Data.Position = transform.position;

			if (m_action is Composite composite) {
				composite.Editor_OrderChildren();
			}
		}

		public void SetEntry(bool isEntry) {
			m_entryLabel.style.display = isEntry ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public void ToggleBreakpoint() {
			bool isBreakpoint = !m_action.Editor_Data.Breakpoint;

			m_action.Editor_Data.Breakpoint = isBreakpoint;
			m_breakpointElement.visible = isBreakpoint;
		}

		private static Texture GetActionIcon(BehaviourAction action) {
			if (action == null)
				return null;

			GraphIconAttribute iconAttribute = (GraphIconAttribute)action.GetType().GetCustomAttribute(typeof(GraphIconAttribute));

			if (iconAttribute == null)
				return null;

			if (s_iconCache.TryGetValue(iconAttribute.Path, out Texture iconTexture))
				return iconTexture;

			iconTexture = Resources.Load<Texture>(iconAttribute.Path);

			if (iconTexture)
				s_iconCache.Add(iconAttribute.Path, iconTexture);

			return iconTexture;
		}
	}
}
