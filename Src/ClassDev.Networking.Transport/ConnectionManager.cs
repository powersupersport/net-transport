using System.Diagnostics;
using System.Net;
using System.Threading;
using ClassDev.Networking.Transport.LowLevel;

namespace ClassDev.Networking.Transport
{
	public class ConnectionManager
	{
		/// <summary>
		/// 
		/// </summary>
		public Connection [] connections { get; private set; }
		/// <summary>
		/// Thread lock for the connections.
		/// </summary>
		private readonly object connectionsLock = new object ();

		/// <summary>
		/// The thread for updating the connections.
		/// </summary>
		private Thread updateThread = null;

		/// <summary>
		/// 
		/// </summary>
		public BaseHandler messageHandler;

		/// <summary>
		/// 
		/// </summary>
		public MessageManager messageManager;

		/// <summary>
		/// 
		/// </summary>
		private MessageChannelTemplate [] channelTemplates = null;

		/// <summary>
		/// 
		/// </summary>
		public int maxConnections { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public Stopwatch stopwatch { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// 
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
		/// 
		/// </summary>
		public void Start ()
		{
			isStarted = true;

			SetupHandlers ();

			SetupConnections ();

			updateThread = new Thread (Threaded_UpdateConnections);
			updateThread.Start ();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop ()
		{
			isStarted = false;

			updateThread.Join ();

			DisposeConnections ();
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
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="disconnected"></param>
		/// <returns></returns>
		public Connection ResolveConnection (IPEndPoint endPoint, bool disconnected = false)
		{
			lock (connectionsLock)
			{
				// TODO: Optimize...
				for (int i = 0; i < connections.Length; i++)
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
		/// Receives a message 
		/// </summary>
		/// <returns></returns>
		public Message Receive ()
		{
			if (connections == null)
				return null;

			Message message = null;
			lock (connectionsLock)
			{
				// TODO: Currently prioritizes the first connection. Should be circular.
				for (int i = 0; i < connections.Length; i++)
				{
					if (connections [i] == null || connections [i].isDisconnected)
						continue;

					message = connections [i].DequeueFromReceive ();
					if (message != null)
						return message;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns a free index for a connection to take over.
		/// </summary>
		/// <returns></returns>
		private int GetFreeConnectionIndex ()
		{
			lock (connectionsLock)
			{
				for (int i = 0; i < connections.Length; i++)
				{
					if (connections [i] == null)
						return i;
				}

				return -1;
			}
		}

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
		private void DisposeConnections ()
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
		/// Thread for updating all connections.
		/// </summary>
		private void Threaded_UpdateConnections ()
		{
			int i = 0;
			Connection connection = null;

			while (true)
			{
				if (!isStarted)
					return;

				for (i = 0; i < maxConnections; i++)
				{
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
			}
		}

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

			message.connection.isSuccessful = true;
		}

		/// <summary>
		/// Handles keep-alive messages.
		/// </summary>
		/// <param name="message"></param>
		private void HandleKeepAliveMessage (Message message)
		{
			if (message.connection == null)
				return;

			// TODO: Should have its own dedicated method.
			if (!message.connection.isSuccessful)
				message.connection.isSuccessful = true;

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
