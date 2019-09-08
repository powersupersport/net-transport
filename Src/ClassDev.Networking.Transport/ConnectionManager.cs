using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using ClassDev.Networking.Transport.LowLevel;

namespace ClassDev.Networking.Transport
{
	public class ConnectionManager
	{
		/// <summary>
		/// Determines how many connections can run on a single thread.
		/// </summary>
		public const int ConnectionsPerThread = 8;

		/// <summary>
		/// Called whenever a connection is successful.
		/// </summary>
		public event System.Action<Connection> OnConnect;
		/// <summary>
		/// Called whenever a connection has been disconnected.
		/// </summary>
		public event System.Action<Connection> OnDisconnect;
		/// <summary>
		/// Queue with all OnConnect events waiting to be invoked on the main thread.
		/// </summary>
		private Queue<Connection> onConnectEvents = new Queue<Connection> ();
		/// <summary>
		/// Queue with all OnDisconnect events waiting to be invoked on the main thread.
		/// </summary>
		private Queue<Connection> onDisconnectEvents = new Queue<Connection> ();
		/// <summary>
		/// Lock for the onConnectEvents queue.
		/// </summary>
		private readonly object onConnectLock = new object ();
		/// <summary>
		/// Lock for the onDisconnectEvents queue.
		/// </summary>
		private readonly object onDisconnectLock = new object ();

		/// <summary>
		/// The allocated space for connections.
		/// </summary>
		public Connection [] connections { get; private set; }
		/// <summary>
		/// Thread lock for the connections.
		/// </summary>
		private readonly object connectionsLock = new object ();
		/// <summary>
		/// The circular index of the current connection when receiving messages.
		/// </summary>
		private int currentReceiveIndex = 0;

		/// <summary>
		/// The threads for updating the connections.
		/// </summary>
		private Thread [] updateThreads = null;

		/// <summary>
		/// The base handler used for registering the necessary callbacks for this connection.
		/// </summary>
		public BaseHandler messageHandler { get; private set; }

		/// <summary>
		/// The message manager used for sending/receiving messages.
		/// </summary>
		public MessageManager messageManager { get; private set; }

		/// <summary>
		/// The default channels used for each connection.
		/// </summary>
		private MessageChannelTemplate [] channelTemplates = null;

		/// <summary>
		/// The maximum connections allowed to connect simultaneously.
		/// </summary>
		public int maxConnections { get; private set; }

		/// <summary>
		/// The stopwatch for tracking time in milliseconds.
		/// </summary>
		public Stopwatch stopwatch { get; private set; }

		/// <summary>
		/// True if the connection manager is active.
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// Constructor for connection manager.
		/// </summary>
		/// <param name="messageManager"></param>
		/// <param name="maxConnections"></param>
		public ConnectionManager (MessageManager messageManager, MessageChannelTemplate [] channelTemplates, BaseHandler messageHandler, Stopwatch stopwatch, int maxConnections)
		{
			if (messageManager == null)
				throw new System.Exception ("Connection manager cannot be created without a message manager.");

			this.messageManager = messageManager;
			this.channelTemplates = channelTemplates;
			this.messageHandler = messageHandler;
			this.maxConnections = maxConnections;

			this.stopwatch = stopwatch;
		}

		/// <summary>
		/// Starts the connection manager.
		/// </summary>
		public void Start ()
		{
			isStarted = true;

			SetupHandlers ();

			SetupConnections ();

			SetupUpdateThreads ();
		}

		/// <summary>
		/// Stops the connection manager. You should call that before closing the application.
		/// </summary>
		public void Stop ()
		{
			isStarted = false;

			TeardownUpdateThreads ();

			TeardownConnections ();
		}

		/// <summary>
		/// Calls any available events. You must call this from the main thread.
		/// </summary>
		public void Update ()
		{
			lock (onConnectLock)
			{
				while (onConnectEvents.Count > 0)
					OnConnect?.Invoke (onConnectEvents.Dequeue ());
			}

			lock (onDisconnectLock)
			{
				while (onDisconnectEvents.Count > 0)
					OnDisconnect?.Invoke (onDisconnectEvents.Dequeue ());
			}
		}

		/// <summary>
		/// Connect to another host.
		/// </summary>
		/// <param name="ipAddress">The IP address of the remote host.</param>
		/// <param name="port">The port of the remote host.</param>
		/// <param name="timeout">For how long the connection attempt will last (in seconds).</param>
		/// <returns>Data about the current state of the connection attempt.</returns>
		public Connection Connect (string ipAddress, int port, float timeout = Connection.ConnectTimeout, MessageChannelTemplate [] channelTemplates = null)
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
		public Connection Connect (IPEndPoint endPoint, float timeout = Connection.ConnectTimeout, MessageChannelTemplate [] channelTemplates = null)
		{
			if (!isStarted)
				return null;

			if (endPoint == null)
				throw new System.ArgumentNullException ("endPoint", "You cannot connect to a null ip end point...");

			Connection connection = ResolveConnection (endPoint);
			if (connection != null)
				return connection;

			if (channelTemplates == null)
				channelTemplates = this.channelTemplates;

			int index = GetFreeConnectionIndex ();
			if (index < 0)
				throw new System.Exception ("Connection limit reached!");

			connection = new Connection (this, channelTemplates, endPoint, index);

			lock (connectionsLock)
			{
				connections [index] = connection;
			}

			return connection;
		}

		/// <summary>
		/// Disconnects a connection.
		/// </summary>
		/// <param name="connection"></param>
		public void Disconnect (Connection connection)
		{
			connection.Disconnect ();
		}

		/// <summary>
		/// Resolves a connection by a specified endpoint. Returns null if there is no connection associated with the provided endpoint.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="disconnected"></param>
		/// <returns></returns>
		public Connection ResolveConnection (IPEndPoint endPoint, bool disconnected = false)
		{
			lock (connectionsLock)
			{
				// TODO: Optimize...
				for (int i = 0; i < maxConnections; i++)
				{
					if (connections [i] == null)
						continue;

					if (!disconnected && connections [i].isDisconnected)
						continue;

					if (Equals (connections [i].endPoint, endPoint))
						return connections [i];
				}

				return null;
			}
		}

		/// <summary>
		/// Receives a message from all connections.
		/// </summary>
		/// <returns></returns>
		public Message Receive ()
		{
			Message message = null;
			lock (connectionsLock)
			{
				if (connections == null)
					return null;

				for (int i = 0; i < maxConnections; i++)
				{
					int index = (i + currentReceiveIndex) % maxConnections;

					if (connections [index] == null || connections [index].isDisconnected)
						continue;

					message = connections [index].DequeueFromReceive ();
					if (message != null)
					{
						currentReceiveIndex = i;
						return message;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns a free index for a connection to take over. It also tries to select it on a free thread.
		/// </summary>
		/// <returns></returns>
		private int GetFreeConnectionIndex ()
		{
			// This math here does the following example...
			// With ConnectionsPerThread = 10 and maxConnections = 30:
			// 0, 10, 20, 1, 11, 21, 2, 12, 22 etc..

			// This is done, so the workload is separated evenly on each thread for maximum performance.

			// If the max connections are not exactly divisible, get the remaining chunk.
			int remainingConnections = maxConnections % ConnectionsPerThread;

			// Rounded means the number is made to be exactly divisible by ConnectionsPerThread.
			int roundedConnections = maxConnections - remainingConnections;

			// If there are remaining connections, then we add an extra chunk to make it exactly divisible.
			if (remainingConnections > 0)
				roundedConnections += ConnectionsPerThread;

			lock (connectionsLock)
			{
				for (int i = 0; i < maxConnections; i++)
				{
					// This one counts as follows: 0,  0,  0,  1,  1,  1,  2,  2,  2 etc...
					int indexToAdd = (i * ConnectionsPerThread) / roundedConnections;

					// This one counts as follows: 0, 10, 20,  0, 10, 20,  0, 10, 20 etc...
					int remainderIndex = (i * ConnectionsPerThread) % roundedConnections;

					// Both combined give us evenly distributed numbers.
					int index = remainderIndex + indexToAdd;

					// Although we must check if the index exceeds the length of the connections as this might happen.
					if (index >= maxConnections)
						continue;

					if (connections [index] == null)
						return index;
				}

				return -1;
			}
		}

		/// <summary>
		/// Enqueues a connection to be passed in an onConnect event.
		/// </summary>
		/// <param name="connection"></param>
		internal void EnqueueToConnectEvents (Connection connection)
		{
			lock (onConnectLock)
			{
				onConnectEvents.Enqueue (connection);
			}
		}

		/// <summary>
		/// Enqueues a connection to be passed in an onDisconnect event.
		/// </summary>
		/// <param name="connection"></param>
		internal void EnqueueToDisconnectEvents (Connection connection)
		{
			lock (onDisconnectLock)
			{
				onDisconnectEvents.Enqueue (connection);
			}
		}

		/// <summary>
		/// Thread for updating a specific chunk of connections.
		/// </summary>
		private void Threaded_UpdateConnections (object rawIndex)
		{
			int index = (int)rawIndex;

			int startIndex = index * ConnectionsPerThread;
			int endIndex = startIndex + ConnectionsPerThread;

			if (endIndex > maxConnections)
				endIndex = maxConnections;

			int i = 0;
			Connection connection = null;

			while (true)
			{
				if (!isStarted)
					return;

				for (i = startIndex; i < endIndex; i++)
				{
					connection = null;

					lock (connectionsLock)
					{
						// In case the connections object gets unreferenced in another thread while it's unlocked.
						if (connections == null)
							break;

						connection = connections [i];

						if (connection == null)
							continue;

						// TODO: Add a constant for the timeout (2000).
						if (connection.isDisconnected && stopwatch.ElapsedMilliseconds - connection.disconnectionTimestamp > 2000)
						{
							connections [i] = null;
							continue;
						}
					}

					try
					{
						connection.Threaded_Update ();
					}
					catch (TimeoutException)
					{
						connection.Disconnect ();
					}
				}

				Thread.Sleep (1);
			}
		}

		#region Setup

		/// <summary>
		/// Sets up the connections array.
		/// </summary>
		private void SetupConnections ()
		{
			lock (connectionsLock)
			{
				connections = new Connection [maxConnections];
			}
		}

		/// <summary>
		/// Disconnects all connections and deallocates the memory.
		/// </summary>
		private void TeardownConnections ()
		{
			lock (connectionsLock)
			{
				for (int i = 0; i < maxConnections; i++)
				{
					if (connections [i] == null)
						continue;

					connections [i].Disconnect ();
				}

				connections = null;
			}
		}

		/// <summary>
		/// Sets up the update threads.
		/// </summary>
		private void SetupUpdateThreads ()
		{
			int length = maxConnections / ConnectionsPerThread;
			if (maxConnections - length > 0)
				length += 1;

			updateThreads = new Thread [length];

			for (int i = 0; i < length; i++)
			{
				updateThreads [i] = new Thread (new ParameterizedThreadStart (Threaded_UpdateConnections));
				updateThreads [i].Start (i);
			}
		}

		/// <summary>
		/// Stops the update threads.
		/// </summary>
		private void TeardownUpdateThreads ()
		{
			for (int i = 0; i < updateThreads.Length; i++)
			{
				if (updateThreads [i] == null)
					continue;

				updateThreads [i].Join ();
			}

			updateThreads = null;
		}

		#endregion

		#region Handlers

		/// <summary>
		/// Sets up all necessary handlers for the connection manager.
		/// </summary>
		private void SetupHandlers ()
		{
			messageHandler.connectionHandler = messageHandler.Register (HandleConnectionMessage);
			messageHandler.keepAliveHandler = messageHandler.Register (HandleKeepAliveMessage);
			messageHandler.disconnectionHandler = messageHandler.Register (HandleDisconnectionMessage);
			messageHandler.acknowledgementHandler = messageHandler.Register (HandleAcknowledgementMessage);
			messageHandler.multipleHandler = messageHandler.Register (HandleMultipleMessage);
			messageHandler.fragmentHandler = messageHandler.Register (HandleFragmentMessage);

			messageHandler.Optimize ();
		}

		/// <summary>
		/// Handles connection messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleConnectionMessage (Message message)
		{
			if (message.connection == null)
			{
				// TODO: Check for channels.
				Connect (message.endPoint);
				return;
			}

			if (!message.connection.isSuccessful)
			{
				message.connection.SetAsSuccessful ();
				EnqueueToConnectEvents (message.connection);
			}
		}

		/// <summary>
		/// Handles keep-alive messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleKeepAliveMessage (Message message)
		{
			if (message.connection == null)
				return;

			int id = 0;
			bool response = false;

			try
			{
				message.encoder.Decode (out id);
				message.encoder.Decode (out response);
			}
			catch (System.Exception)
			{
				return;
			}

			if (response)
			{
				message.connection.HandleKeepAlive (id);
				return;
			}

			Message newMessage = new Message (message.connection, messageHandler.keepAliveHandler, 0, 10);
			newMessage.encoder.Encode (id);
			newMessage.encoder.Encode (true);
			message.connection.EnqueueToSend (newMessage);
		}

		/// <summary>
		/// Handles disconnection messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleDisconnectionMessage (Message message)
		{
			if (message.connection == null)
				return;

			message.connection.Disconnect ();
		}

		/// <summary>
		/// Handles acknowledgement messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleAcknowledgementMessage (Message message)
		{
			if (message.connection == null)
				return;

			byte channelId = 0;
			int sequenceIndex = 0;

			try
			{
				message.encoder.Decode (out channelId);
				message.encoder.Decode (out sequenceIndex);
			}
			catch (System.Exception)
			{
				return;
			}

			MessageChannel channel = message.connection.GetMessageChannelByIndex (channelId);
			if (channel == null)
				return;

			if (!channel.isReliable)
				return;

			((ReliableMessageChannel)channel).Acknowledge (sequenceIndex);
		}

		/// <summary>
		/// Handles zipped (combined) messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleMultipleMessage (Message message)
		{

		}

		/// <summary>
		/// Handles fragmented messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleFragmentMessage (Message message)
		{

		}

		#endregion
	}
}
