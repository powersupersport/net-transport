﻿using System.Net;
using System.Threading;
using ClassDev.Networking.Transport.LowLevel;

namespace ClassDev.Networking.Transport
{
	public class ConnectionManager
	{
		/// <summary>
		/// 
		/// </summary>
		private BaseHandler messageHandler;

		/// <summary>
		/// 
		/// </summary>
		private MessageManager messageManager;

		/// <summary>
		/// 
		/// </summary>
		public Connection [] connections = null;

		/// <summary>
		/// 
		/// </summary>
		private Thread thread;

		/// <summary>
		/// 
		/// </summary>
		public int maxConnections = 1;

		/// <summary>
		/// 
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="messageManager"></param>
		/// <param name="maxConnections"></param>
		public ConnectionManager (MessageManager messageManager, BaseHandler messageHandler, int maxConnections)
		{
			if (messageManager == null)
				throw new System.Exception ("Connection manager cannot be created without a message manager.");

			this.messageManager = messageManager;
			this.messageHandler = messageHandler;
			this.maxConnections = maxConnections;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Start ()
		{
			isStarted = true;

			SetupHandlers ();

			SetupConnections ();

			thread = new Thread (Threaded_UpdateConnections);
			thread.Start ();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop ()
		{
			isStarted = false;

			thread.Join ();

			DisposeConnections ();
		}

		/// <summary>
		/// Connect to another host.
		/// </summary>
		/// <param name="ipAddress">The IP address of the remote host.</param>
		/// <param name="port">The port of the remote host.</param>
		/// <param name="timeout">For how long the connection attempt will last (in seconds).</param>
		/// <returns>Data about the current state of the connection attempt.</returns>
		public Connection Connect (string ipAddress, int port, float timeout = Connection.Timeout)
		{
			if (!isStarted)
				return null;

			// If invalid, it will already throw an exception. No need to manually check for it.
			IPAddress ipAddressParsed = IPAddress.Parse (ipAddress);

			IPEndPoint endPoint = new IPEndPoint (ipAddressParsed, port);

			return Connect (endPoint, timeout);
		}
		/// <summary>
		/// Connect to another host.
		/// </summary>
		/// <param name="endPoint">The remote end point to connect to.</param>
		/// <param name="timeout">For how long the connection attempt will last (in seconds).</param>
		/// <returns>Data about the current state of the connection attempt.</returns>
		public Connection Connect (IPEndPoint endPoint, float timeout = Connection.Timeout)
		{
			if (!isStarted)
				return null;

			Connection connection = new Connection (messageManager, messageHandler.connectionHandler, endPoint);
			connections [0] = connection;

			return connection;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		public Connection ResolveConnection (IPEndPoint endPoint)
		{
			// TODO: Optimize...
			for (int i = 0; i < connections.Length; i++)
			{
				if (connections [i] == null)
					continue;

				if (Equals (connections [i].endPoint, endPoint))
					return connections [i];
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		private void SetupConnections ()
		{
			connections = new Connection [maxConnections];
		}

		/// <summary>
		/// 
		/// </summary>
		private void DisposeConnections ()
		{
			for (int i = 0; i < maxConnections; i++)
			{
				if (connections [i] == null)
					continue;

				connections [i].Disconnect ();
			}

			connections = null;
		}

		/// <summary>
		/// 
		/// </summary>
		private void Threaded_UpdateConnections ()
		{
			int i = 0;

			while (true)
			{
				if (!isStarted)
					return;

				for (i = 0; i < maxConnections; i++)
				{
					if (connections [i] == null)
						continue;

					connections [i].Update ();
				}
			}
		}

		#region Handlers

		private void SetupHandlers ()
		{
			messageHandler.connectionHandler = messageHandler.Register (HandleConnectionMessage);
			messageHandler.disconnectionHandler = messageHandler.Register (HandleDisconnectionMessage);
			messageHandler.acknowledgementHandler = messageHandler.Register (HandleAcknowledgementMessage);
			messageHandler.multipleHandler = messageHandler.Register (HandleMultipleMessage);
			messageHandler.fragmentHandler = messageHandler.Register (HandleFragmentMessage);

			messageHandler.Optimize ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void HandleConnectionMessage (Message message)
		{
			if (message.connection == null)
			{
				Connect (message.endPoint);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void HandleDisconnectionMessage (Message message)
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void HandleAcknowledgementMessage (Message message)
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void HandleMultipleMessage (Message message)
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void HandleFragmentMessage (Message message)
		{

		}

		#endregion
	}
}
