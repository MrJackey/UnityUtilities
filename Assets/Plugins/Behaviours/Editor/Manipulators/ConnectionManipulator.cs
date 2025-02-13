using System;
using Jackey.Behaviours.Editor.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Jackey.Behaviours.Editor.Manipulators {
	public class ConnectionManipulator : MouseManipulator {
		private bool m_active;

		private VisualElement m_container;

		private Connection m_connection;
		private IConnectionSocket m_socket;

		public delegate bool ConnectionValidHandler(IConnectionSocket start, IConnectionSocket end);
		public ConnectionValidHandler ConnectionValidator;

		public delegate void ConnectionVoidedHandler(Connection connection, Action<Connection, IConnectionSocket> restore);
		public event ConnectionVoidedHandler ConnectionVoided;

		public delegate void ConnectionCreatedHandler(Connection connection);
		public event ConnectionCreatedHandler ConnectionCreated;

		public delegate void ConnectionMovedHandler(Connection connection, IConnectionSocket from, IConnectionSocket to);
		public event ConnectionMovedHandler ConnectionMoved;

		public delegate void ConnectionRemovedHandler(Connection connection, IConnectionSocket start, IConnectionSocket end);
		public event ConnectionRemovedHandler ConnectionRemoved;

		public ConnectionManipulator(VisualElement container) {
			m_container = container;
		}

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}

		public void CreateConnection(IConnectionSocket socket) {
			if (m_active) return;

			m_connection = new Connection();
			m_container.Add(m_connection);

			m_connection.Start = socket;
			socket.OutgoingConnections++;

			m_active = true;
			target.CaptureMouse();
		}

		public void MoveConnection(Connection connection, IConnectionSocket socket) {
			if (m_active) return;

			m_connection = connection;
			m_socket = socket;

			if (m_socket == m_connection.Start) {
				m_connection.Start = null;
				m_socket.OutgoingConnections--;
			}
			else {
				m_connection.End = null;
				m_socket.IncomingConnections--;
			}

			m_active = true;
			target.CaptureMouse();
		}

		private void OnMouseMove(MouseMoveEvent evt) {
			if (!m_active) return;

			m_connection.MarkDirtyRepaint();
		}

		private void OnMouseUp(MouseUpEvent evt) {
			if (!m_active) return;

			if (m_socket == null) {
				EndCreate(evt);
			}
			else {
				EndMove(evt);
			}

			target.ReleaseMouse();
			m_connection = null;
			m_socket = null;
			m_active = false;
		}

		private void EndCreate(MouseUpEvent evt) {
			Vector2 mousePosition = evt.localMousePosition;
			bool voided = true;

			foreach (VisualElement child in target.Children()) {
				if (child is not IConnectionSocketOwner socketOwner)
					continue;

				for (int i = socketOwner.Sockets.Count - 1; i >= 0; i--) {
					IConnectionSocket socket = socketOwner.Sockets[i];
					VisualElement element = socket.Element;

					Vector2 socketMousePosition = target.ChangeCoordinatesTo(element, mousePosition);

					if (!element.ContainsPoint(socketMousePosition))
						continue;

					bool isConnectionValid = ConnectionValidator?.Invoke(m_connection.Start, socket) ?? true;
					if (!isConnectionValid) {
						voided = false;
						continue;
					}

					voided = false;

					if (socket.IncomingConnections >= socket.MaxIncomingConnections)
						continue;

					m_connection.End = socket;
					socket.IncomingConnections++;

					ConnectionCreated?.Invoke(m_connection);
					return;
				}
			}

			if (m_connection.End == null) {
				m_connection.Start.OutgoingConnections--;
				m_connection.RemoveFromHierarchy();

				if (voided)
					ConnectionVoided?.Invoke(m_connection, RestoreCancel);
			}
		}

		private void RestoreCancel(Connection connection, IConnectionSocket to) {
			m_container.Add(connection);

			connection.Start.OutgoingConnections++;

			connection.End = to;
			to.IncomingConnections++;

			ConnectionCreated?.Invoke(connection);
		}

		private void EndMove(MouseUpEvent evt) {
			Vector2 mousePosition = evt.localMousePosition;

			foreach (VisualElement child in target.Children()) {
				if (child is not IConnectionSocketOwner socketOwner)
					continue;

				for (int i = socketOwner.Sockets.Count - 1; i >= 0; i--) {
					IConnectionSocket socket = socketOwner.Sockets[i];
					VisualElement element = socket.Element;

					Vector2 socketMousePosition = target.ChangeCoordinatesTo(element, mousePosition);

					if (!element.ContainsPoint(socketMousePosition))
						continue;

					bool isConnectionValid = ConnectionValidator != null &&
					                         (m_connection.Start != null
						                         ? ConnectionValidator.Invoke(m_connection.Start, socket)
						                         : ConnectionValidator.Invoke(socket, m_connection.End));
					if (!isConnectionValid)
						continue;

					// Moved to same socket
					if (socket == m_socket) {
						if (m_connection.Start == null) {
							Debug.Assert(socket.MaxOutgoingConnections == -1 || socket.OutgoingConnections < socket.MaxOutgoingConnections);

							m_connection.Start = socket;
							socket.OutgoingConnections++;
						}
						else {
							Debug.Assert(socket.MaxIncomingConnections == -1 || socket.IncomingConnections < socket.MaxIncomingConnections);

							m_connection.End = socket;
							socket.IncomingConnections++;
						}

						return;
					}

					// Moved to new socket
					if (m_connection.Start == null) {
						if (socket.MaxOutgoingConnections != -1 && socket.OutgoingConnections >= socket.MaxOutgoingConnections)
							continue;

						m_connection.Start = socket;
						socket.OutgoingConnections++;
					}
					else {
						if (socket.IncomingConnections != -1 && socket.IncomingConnections >= socket.MaxIncomingConnections)
							continue;

						m_connection.End = socket;
						socket.IncomingConnections++;
					}

					ConnectionMoved?.Invoke(m_connection, m_socket, socket);
					return;
				}
			}

			if (m_connection.Start == null) {
				m_connection.End.IncomingConnections--;
				m_connection.RemoveFromHierarchy();
				ConnectionRemoved?.Invoke(m_connection, m_socket, m_connection.End);
			}
			else if (m_connection.End == null) {
				m_connection.Start.OutgoingConnections--;
				m_connection.RemoveFromHierarchy();
				ConnectionRemoved?.Invoke(m_connection, m_connection.Start, m_socket);
			}
		}
	}
}
