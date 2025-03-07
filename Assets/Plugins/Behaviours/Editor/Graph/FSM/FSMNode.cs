using Jackey.Behaviours.BT;
using Jackey.Behaviours.Editor.Utilities;
using Jackey.Behaviours.FSM;
using Jackey.Behaviours.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Graph.FSM {
	public class FSMNode : Node, ITickElement {
		private static readonly Texture ICON_TEXTURE = Resources.Load<Texture>("BehaviourTree");

		private BehaviourState m_state;
		private ActionStatus m_lastRuntimeStatus = ActionStatus.Inactive;

		private Label m_label;

		private Label m_entryLabel;
		private VisualElement m_breakpointElement;

		public BehaviourState State => m_state;

		public FSMNode(BehaviourState state) {
			m_state = state;

			hierarchy.Add(m_entryLabel = new Label("Entry") {
				name = "Entry",
			});
			m_entryLabel.SendToBack();
			Add(m_breakpointElement = new VisualElement() { name = "Breakpoint" });
			Add(new Image() {
				name = "Icon",
				pickingMode = PickingMode.Ignore,
				scaleMode = ScaleMode.ScaleToFit,
				image = ICON_TEXTURE,
			});
			Add(m_label = new Label() {
				pickingMode = PickingMode.Ignore,
			});

			usageHints = UsageHints.DynamicTransform;
			transform.position = state.Editor_Data.Position;
		}

		public void Tick() {
			RefreshInfo();
			UpdateEditorData();

			if (EditorApplication.isPlaying)
				RuntimeTick();
		}

		private void RefreshInfo() {
			string info = m_state.Editor_Info;

			if (string.IsNullOrEmpty(info)) {
				info = m_state.GetType().GetDisplayOrTypeName();

				if (string.IsNullOrEmpty(info))
					m_label.style.display = DisplayStyle.None;
				else
					m_label.style.display = DisplayStyle.Flex;
			}
			else {
				m_label.style.display = DisplayStyle.Flex;
			}

			m_label.text = info;

			m_breakpointElement.visible = m_state.Editor_Data.Breakpoint;
		}

		private void UpdateEditorData() {
			m_state.Editor_Data.Position = transform.position;
		}

		private void RuntimeTick() {
			if (m_state.Status == m_lastRuntimeStatus)
				return;

			string previousClass = m_lastRuntimeStatus switch {
				ActionStatus.Running => "Status-Running",
				ActionStatus.Success => "Status-Success",
				ActionStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(previousClass))
				contentContainer.RemoveFromClassList(previousClass);

			string nextClass = m_state.Status switch {
				ActionStatus.Running => "Status-Running",
				ActionStatus.Success => "Status-Success",
				ActionStatus.Failure => "Status-Failure",
				_ => null,
			};

			if (!string.IsNullOrEmpty(nextClass))
				contentContainer.EnsureClass(nextClass);

			m_lastRuntimeStatus = m_state.Status;
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
