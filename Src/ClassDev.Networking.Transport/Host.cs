using ClassDev.Networking.Transport.LowLevel;
using System.Net.Sockets;
using System.Net;
using System;

namespace ClassDev.Networking.Transport
{
	public class Host
	{
		/// <summary>
		/// True if the host is running (sends and listens for messages)
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// The UDP client used for sending and receiving messages.
		/// </summary>
		public UdpClient udpClient { get; private set; }
		/// <summary>
		/// The local endpoint.
		/// </summary>
		public IPEndPoint endPoint { get; private set; }
		/// <summary>
		/// The message manager used for managing sending and receiving messages.
		/// </summary>
		public MessageManager messageManager { get; private set; }
		/// <summary>
		/// The connection manager used for managing connections.
		/// </summary>
		public ConnectionManager connectionManager { get; private set; }
		/// <summary>
		/// The message handler used to handle messages.
		/// </summary>
		public BaseHandler messageHandler { get; private set; }

		/// <summary>
		/// Defines the default channels for each connection.
		/// </summary>
		public MessageChannelTemplate [] channelTemplates { get; set; }

		/// <summary>
		/// The port on which the UDP client is running.
		/// </summary>
		public int port { get; private set; }

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Host () : this (0)
		{

		}
		/// <summary>
		/// Constructor with port.
		/// </summary>
		public Host (int port)
		{
			this.port = port;
		}

		/// <summary>
		/// Starts the host.
		/// </summary>
		public void Start ()
		{
			isStarted = true;

			// TODO: The message handler should be setup before starting the host.
			SetupMessageHandler ();

			SetupUdpClient ();

			SetupMessageManager ();

			SetupConnectionManager ();
		}

		/// <summary>
		/// Stops the host.
		/// </summary>
		public void Stop ()
		{
			isStarted = false;

			ShutdownMessageHandler ();

			ShutdownConnectionManager ();

			ShutdownMessageManager ();

			ShutdownUdpClient ();
		}

		/// <summary>
		/// Sends a message from the queue.
		/// </summary>
		/// <param name="message"></param>
		public void Send (Message message)
		{
			if (message == null)
				// TODO: Throw an exception.
				return;

			if (message.connection != null)
				message.connection.EnqueueToSend (message);
		}

		/// <summary>
		/// A function to update the synchronous part of the code.
		/// </summary>
		public void Update ()
		{
			Receive ();
		}

		[Obsolete]
		/// <summary>
		/// Receives a message from the queue.
		/// </summary>
		/// <returns></returns>
		public Message Receive ()
		{
			LowLevel.Message lowLevelMessage = messageManager.Receive ();

			if (lowLevelMessage == null)
				return null;

			Message message = new Message (lowLevelMessage);

			message.connection = connectionManager.ResolveConnection (message.endPoint);
			if (message.connection != null)
			{
				// The message channel ID is assigned in the message itself.
				message.channel = message.connection.GetMessageChannelByIndex (message.channelId);
				if (message.channel == null)
					return null;

				message.connection.EnqueueToReceive (message);
				message = message.connection.DequeueFromReceive ();
			}

			if (message == null)
				return null;

			messageHandler.Handle (message);

			return message;
		}

		// ---------------------------------------------------------------------------

		#region Setup

		/// <summary>
		/// Initializes the UDP client.
		/// </summary>
		private void SetupUdpClient ()
		{
			if (udpClient != null)
				return;

			IPAddress ipAddress = new IPAddress (new byte [] { 0, 0, 0, 0 });
			endPoint = new IPEndPoint (ipAddress, port);

			udpClient = new UdpClient (endPoint);

			endPoint.Port = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;

			// TODO: Has to be changeable from the settings.
			udpClient.Client.ReceiveTimeout = 50;
		}

		/// <summary>
		/// Closes the UDP client port and disposes the object.
		/// </summary>
		private void ShutdownUdpClient ()
		{
			if (udpClient == null)
				return;

			udpClient.Close ();
			udpClient.Dispose ();
			udpClient = null;
		}

		/// <summary>
		/// Initializes the message manager.
		/// </summary>
		private void SetupMessageManager ()
		{
			if (messageManager != null)
				ShutdownMessageManager ();

			if (udpClient == null)
				throw new Exception ("Cannot set up message manager if the UdpClient is null.");

			messageManager = new MessageManager (udpClient);
			messageManager.Start ();
		}

		/// <summary>
		/// Stops the message manager and resets the variable.
		/// </summary>
		private void ShutdownMessageManager ()
		{
			if (messageManager == null)
				return;

			messageManager.Stop ();
			messageManager = null;
		}

		/// <summary>
		/// Initializes the connection manager.
		/// </summary>
		private void SetupConnectionManager ()
		{
			if (connectionManager != null)
				ShutdownConnectionManager ();

			if (messageManager == null)
				throw new Exception ("Cannot set up connection manager if the message manager is null.");

			connectionManager = new ConnectionManager (messageManager, channelTemplates, messageHandler, 10);
			connectionManager.Start ();
		}

		/// <summary>
		/// Stops the connection manager and resets the variable.
		/// </summary>
		private void ShutdownConnectionManager ()
		{
			if (connectionManager == null)
				return;

			connectionManager.Stop ();
			connectionManager = null;
		}

		/// <summary>
		/// Creates a new base handler and assigns the default callbacks.
		/// </summary>
		private void SetupMessageHandler ()
		{
			messageHandler = new BaseHandler ();

			messageHandler.genericHandler = messageHandler.Register (HandleGenericMessage);

			messageHandler.Optimize ();
		}

		/// <summary>
		/// Unreferences the message handler.
		/// </summary>
		private void ShutdownMessageHandler ()
		{
			messageHandler = null;
		}

		#endregion

		#region Callbacks

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		private void HandleGenericMessage (Message message)
		{

		}

		#endregion
	}
}
