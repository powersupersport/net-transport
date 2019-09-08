using ClassDev.Networking.Transport.LowLevel;
using System.Net.Sockets;
using System.Net;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace ClassDev.Networking.Transport
{
	public class Host
	{
		/// <summary>
		/// True if the host is running (sends and listens for messages)
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// [CAUTION] The heart, the core, the client used for sending and receiving messages. Use ONLY if you cannot utilize the MessageManager included in this class.
		/// </summary>
		public UdpClient udpClient { get; private set; }
		/// <summary>
		/// The local endpoint.
		/// </summary>
		public IPEndPoint endPoint { get; private set; }
		/// <summary>
		/// The message manager used for managing sending and receiving messages. Use only if no methods in this class fit your needs.
		/// </summary>
		public MessageManager messageManager { get; private set; }

		/// <summary>
		/// The connection manager used for managing connections. Use only if no methods in this class fit your needs.
		/// </summary>
		public ConnectionManager connectionManager { get; private set; }
		/// <summary>
		/// The maximum number of connections for the connection manager.
		/// </summary>
		public int maxConnections { get; private set; }
		/// <summary>
		/// Called whenever a connection is successful (alias for connectionManager.OnConnect).
		/// </summary>
		public event Action<Connection> OnConnect
		{
			add => connectionManager.OnConnect += value;
			remove => connectionManager.OnConnect -= value;
		}
		/// <summary>
		/// Called whenever a connection is disconnected (alias for connectionManager.OnDisconnect).
		/// </summary>
		public event Action<Connection> OnDisconnect
		{
			add => connectionManager.OnDisconnect += value;
			remove => connectionManager.OnDisconnect -= value;
		}

		/// <summary>
		/// The message handler used to handle messages.
		/// </summary>
		public BaseHandler messageHandler { get; private set; }
		/// <summary>
		/// A struct for the result of a resolved handler used in async handler resolving.
		/// </summary>
		private struct ReceivedMessage
		{
			public Message message;
			public MessageHandler.Callback callback;

			public ReceivedMessage (Message message, MessageHandler.Callback callback)
			{
				this.message = message;
				this.callback = callback;
			}

			public void Handle ()
			{
				callback (message);
			}
		}
		/// <summary>
		/// Queue of messages with resolved handlers.
		/// </summary>
		private Queue<ReceivedMessage> receivedMessages = new Queue<ReceivedMessage> ();
		/// <summary>
		/// Thread lock for the received messages.
		/// </summary>
		private readonly object receivedMessagesLock = new object ();
		/// <summary>
		/// Thread to run the resolving of incoming messages.
		/// </summary>
		private Thread receiveThread = null;

		/// <summary>
		/// Keeps track of time in milliseconds.
		/// </summary>
		public Stopwatch stopwatch { get; private set; }

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
		public Host (int maxConnections) : this (0, maxConnections)
		{

		}
		/// <summary>
		/// Constructor with port.
		/// </summary>
		public Host (int port, int maxConnections)
		{
			this.port = port;
			this.maxConnections = maxConnections;
		}

		/// <summary>
		/// Starts the host.
		/// </summary>
		public void Start ()
		{
			if (isStarted)
				return;

			isStarted = true;

			SetupStopwatch ();

			SetupMessageHandler ();

			SetupUdpClient ();

			SetupMessageManager ();

			SetupConnectionManager ();

			SetupReceiveThread ();
		}

		/// <summary>
		/// Stops the host.
		/// </summary>
		public void Stop ()
		{
			if (!isStarted)
				return;

			isStarted = false;

			TeardownReceiveThread ();

			TeardownMessageHandler ();

			TeardownConnectionManager ();

			TeardownMessageManager ();

			TeardownUdpClient ();

			TeardownStopwatch ();

			GC.Collect ();
		}

		/// <summary>
		/// Call this from a synchronous update loop in your app.
		/// </summary>
		public void Update ()
		{
			lock (receivedMessagesLock)
			{
				while (receivedMessages.Count > 0)
				{
					receivedMessages.Dequeue ().Handle ();
				}
			}

			connectionManager.Update ();
		}

		/// <summary>
		/// Sends a message.
		/// </summary>
		/// <param name="message"></param>
		public void Send (Message message)
		{
			if (message == null)
				throw new ArgumentNullException ("message", "The specified message to send is null.");

			SendPrivate (message);
		}

		/// <summary>
		/// Connect to a host using an IP address and a port.
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="port"></param>
		/// <param name="timeout"></param>
		/// <param name="messageChannelTemplates"></param>
		/// <returns></returns>
		public Connection Connect (string ipAddress, int port, float timeout = Connection.ConnectTimeout, MessageChannelTemplate [] messageChannelTemplates = null)
		{
			if (!isStarted)
				throw new InvalidOperationException ("You cannot use Connect if the host isn't started.");

			return connectionManager.Connect (ipAddress, port, timeout, messageChannelTemplates);
		}
		/// <summary>
		/// Connect to a host using an IPEndPoint.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout"></param>
		/// <param name="messageChannelTemplates"></param>
		/// <returns></returns>
		public Connection Connect (IPEndPoint endPoint, float timeout = Connection.ConnectTimeout, MessageChannelTemplate [] messageChannelTemplates = null)
		{
			if (!isStarted)
				throw new InvalidOperationException ("You cannot use Connect if the host isn't started.");

			return connectionManager.Connect (endPoint, timeout, messageChannelTemplates);
		}

		/// <summary>
		/// Disconnects a connection.
		/// </summary>
		/// <param name="connection"></param>
		public void Disconnect (Connection connection)
		{
			if (!isStarted)
				throw new InvalidOperationException ("You cannot use Disconnect if the host isn't started.");

			if (connection == null)
				throw new NullReferenceException ("The connection you want to disconnect is null.");

			connectionManager.Disconnect (connection);
		}

		/// <summary>
		/// Resolves a connection by an IP address and a port.
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		public Connection ResolveConnection (string ipAddress, int port)
		{
			IPAddress ipAddressParsed = IPAddress.Parse (ipAddress);
			IPEndPoint endPoint = new IPEndPoint (ipAddressParsed, port);

			return ResolveConnection (endPoint);
		}
		/// <summary>
		/// Resolves a connection by an IPEndPoint.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		public Connection ResolveConnection (IPEndPoint endPoint)
		{
			return connectionManager.ResolveConnection (endPoint);
		}

		// ---------------------------------------------------------------------------

		#region Setup

		/// <summary>
		/// Starts the stopwatch.
		/// </summary>
		private void SetupStopwatch ()
		{
			stopwatch = new Stopwatch ();
			stopwatch.Start ();
		}

		/// <summary>
		/// Stops the stopwatch.
		/// </summary>
		private void TeardownStopwatch ()
		{
			stopwatch.Stop ();
			stopwatch = null;
		}

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
		}

		/// <summary>
		/// Closes the UDP client port and disposes the object.
		/// </summary>
		private void TeardownUdpClient ()
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
				TeardownMessageManager ();

			if (udpClient == null)
				throw new Exception ("Cannot set up message manager if the UdpClient is null.");

			messageManager = new MessageManager (udpClient);
		}

		/// <summary>
		/// Stops the message manager and resets the variable.
		/// </summary>
		private void TeardownMessageManager ()
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
				TeardownConnectionManager ();

			if (messageManager == null)
				throw new Exception ("Cannot set up connection manager if the message manager is null.");

			connectionManager = new ConnectionManager (messageManager, channelTemplates, messageHandler, stopwatch, 10);
			connectionManager.Start ();
		}

		/// <summary>
		/// Stops the connection manager and resets the variable.
		/// </summary>
		private void TeardownConnectionManager ()
		{
			if (connectionManager == null)
				return;

			connectionManager.Stop ();

			connectionManager.Update ();

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
		private void TeardownMessageHandler ()
		{
			messageHandler = null;
		}

		/// <summary>
		/// Starts the thread for receiving incoming messages.
		/// </summary>
		private void SetupReceiveThread ()
		{
			receiveThread = new Thread (Threaded_ReceiveMessages);
			receiveThread.Start ();
		}

		/// <summary>
		/// Stops the thread for receiving incoming messages.
		/// </summary>
		private void TeardownReceiveThread ()
		{
			receiveThread.Join ();
			receiveThread = null;
		}

		#endregion

		#region Messages

		// This section is for receiving messages and resolving their handlers asynchronously.

		/// <summary>
		/// Enqueues a message to the message manager's queue
		/// </summary>
		/// <param name="message"></param>
		private void SendPrivate (Message message)
		{
			// If the message is associated with a connection
			if (message.connection != null)
			{
				message.connection.EnqueueToSend (message);
				return;
			}

			// Else just send it freely.
			messageManager.Send (message);
		}

		/// <summary>
		/// Continuously receives messages.
		/// </summary>
		private void Threaded_ReceiveMessages ()
		{
			LowLevel.Message lowLevelMessage = null;
			Message message = null;

			while (true)
			{
				if (!isStarted)
					return;

				// If there are messages in the queues of connections.
				message = connectionManager.Receive ();
				if (message != null)
					ResolveHandlerOfMessage (message);

				// If there are messages in the message buffer.
				message = null;
				lowLevelMessage = messageManager.Receive ();
				if (lowLevelMessage != null)
				{
					message = new Message (lowLevelMessage);
					HandleReceivedMessage (message);
				}
			}
		}

		/// <summary>
		/// Resolves the handler associated with this message and adds it to the resolved handlers.
		/// </summary>
		/// <param name="message"></param>
		private void ResolveHandlerOfMessage (Message message)
		{
			MessageHandler handler = messageHandler.ResolveHandler (message);
			if (handler != null)
			{
				lock (receivedMessagesLock)
					receivedMessages.Enqueue (new ReceivedMessage (message, handler.callback));
			}
		}

		/// <summary>
		/// Must be called after a message has been received from the message manager.
		/// </summary>
		/// <param name="message"></param>
		private void HandleReceivedMessage (Message message)
		{
			message.connection = connectionManager.ResolveConnection (message.endPoint, true);
			if (message.connection != null && !message.connection.isDisconnected)
			{
				// The message channel ID is assigned in the message itself.
				message.channel = message.connection.GetMessageChannelByIndex (message.channelId);
				if (message.channel != null)
				{
					message.connection.EnqueueToReceive (message);
					message = message.connection.DequeueFromReceive ();
				}
				else
				{
					// If the channel is null, then the message is invalid.
					message = null;
				}
			}

			if (message != null)
				ResolveHandlerOfMessage (message);
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
