using ClassDev.Networking.Transport.LowLevel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace ClassDev.Networking.Transport
{
	public class Connection
	{
		/// <summary>
		/// The waiting time before a connection is considered as unsuccessfull after starting a connect action.
		/// </summary>
		public const int ConnectTimeout = 10000;
		/// <summary>
		/// The waiting time before a connection is considered disconnected if no packets are received.
		/// </summary>
		public const int DisconnectTimeout = 3000;
		/// <summary>
		/// The waiting time between each keep-alive packet in milliseconds.
		/// </summary>
		public const int Frequency = 200;

		/// <summary>
		/// The connection identifier.
		/// </summary>
		public int id { get; private set; }

		/// <summary>
		/// True if a connection has been established successfully.
		/// Note that this will still be true if the connection is later on disconnected.
		/// </summary>
		public bool isSuccessful { get; private set; }
		/// <summary>
		/// True if the connection is no longer active.
		/// </summary>
		public bool isDisconnected { get; private set; }
		/// <summary>
		/// The timestamp of when the disconnection occurrs.
		/// </summary>
		public long disconnectionTimestamp { get; private set; }
		/// <summary>
		/// True if the OnDisconnect event has been called.
		/// </summary>
		internal bool disconnectEventCalled { get; private set; }

		/// <summary>
		/// The message manager used for sending messages.
		/// </summary>
		private MessageManager messageManager;
		/// <summary>
		/// The remote end point of this connection.
		/// </summary>
		public IPEndPoint endPoint { get; private set; }

		/// <summary>
		/// The channels used for managing reliability/order of messages.
		/// </summary>
		public MessageChannel [] channels { get; private set; }
		/// <summary>
		/// Used when enqueueing/dequeueing outgoing messages.
		/// </summary>
		private readonly object sendChannelLock = new object ();
		/// <summary>
		/// Used when enqueueing/dequeueing incoming messages.
		/// </summary>
		private readonly object receiveChannelLock = new object ();

		/// <summary>
		/// Used when sending connection messages.
		/// </summary>
		private MessageHandler connectionHandler;
		/// <summary>
		/// Used when sending keep-alive messages.
		/// </summary>
		private MessageHandler keepAliveHandler;
		/// <summary>
		/// Used when sending acknowledgement messages.
		/// </summary>
		private MessageHandler acknowledgementHandler;
		/// <summary>
		/// Used when sending disconnect messages.
		/// </summary>
		private MessageHandler disconnectionHandler;

		/// <summary>
		/// Keeps the id of the keep-alive packet and its timestamp when it was sent.
		/// </summary>
		private struct KeepAlive
		{
			public int id;
			public long timestamp;
		}
		/// <summary>
		/// Keeps track of sent keep-alive packets to compare for ping.
		/// </summary>
		private CircularArray<KeepAlive> keepAlives = new CircularArray<KeepAlive> (10);
		/// <summary>
		/// Thread lock for keep alive records.
		/// </summary>
		private readonly object keepAlivesLock = new object ();

		/// <summary>
		/// The id of the last sent keep-alive packet (increments on every packet sent).
		/// </summary>
		private int currentKeepAliveId = 0;
		/// <summary>
		/// The timestamp in milliseconds of the last sent keep-alive packet (used to track when to send another packet).
		/// </summary>
		private long currentKeepAliveTimestamp = 0;
		/// <summary>
		/// The timestamp in milliseconds of the latest packet received.
		/// </summary>
		private long latestPacketReceivedTimestamp = 0;

		/// <summary>
		/// The ping of the latest packet.
		/// </summary>
		public int latestPing { get; private set; }
		/// <summary>
		/// The averaged ping over two consequetive packets.
		/// </summary>
		public int averagePing { get; private set; }
		/// <summary>
		/// Alias for averagePing.
		/// </summary>
		public int ping
		{
			get => averagePing;
		}

		/// <summary>
		/// The stopwatch for tracking time in milliseconds.
		/// </summary>
		public readonly Stopwatch stopwatch;

		/// <summary>
		/// The circular index used to browse progressively through the channels and dequeue messages to send.
		/// </summary>
		private int currentSendChannelIndex = 0;

		/// <summary>
		/// The circular index used to browse progressively through the channels and dequeue messages to receive.
		/// </summary>
		private int currentReceiveChannelIndex = 0;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="messageManager"></param>
		/// <param name="endPoint"></param>
		public Connection (ConnectionManager connectionManager, MessageChannelTemplate [] channelTemplates, IPEndPoint endPoint, int id)
		{
			this.id = id;

			messageManager = connectionManager.messageManager;
			connectionHandler = connectionManager.messageHandler.connectionHandler;
			keepAliveHandler = connectionManager.messageHandler.keepAliveHandler;
			disconnectionHandler = connectionManager.messageHandler.disconnectionHandler;
			acknowledgementHandler = connectionManager.messageHandler.acknowledgementHandler;

			stopwatch = connectionManager.stopwatch;
			latestPacketReceivedTimestamp = stopwatch.ElapsedMilliseconds;

			if (channelTemplates == null)
				channelTemplates = new MessageChannelTemplate [0];

			channels = new MessageChannel [channelTemplates.Length + 1];
			channels [0] = new MessageChannel (this, 0);

			for (int i = 0; i < channelTemplates.Length; i++)
			{
				if (channelTemplates [i].isReliable)
					channels [i + 1] = new ReliableMessageChannel (this, messageManager, acknowledgementHandler, stopwatch, (byte)(i + 1), channelTemplates [i].isSequenced);
				else
					channels [i + 1] = new MessageChannel (this, (byte)(i + 1), channelTemplates [i].isSequenced);
			}

			this.endPoint = endPoint;
		}

		// TODO: MUST BE THREAD SAFE!
		/// <summary>
		/// Disconnects the connection.
		/// </summary>
		public void Disconnect ()
		{
			if (isDisconnected)
				return;

			isDisconnected = true;
			disconnectionTimestamp = stopwatch.ElapsedMilliseconds;

			for (int i = 0; i < 3; i++)
			{
				SendDisconnectionMessage ();
			}
		}

		/// <summary>
		/// Returns the channel instance by the specified index.
		/// </summary>
		/// <param name="index">The channel index.</param>
		/// <returns></returns>
		public MessageChannel GetMessageChannelByIndex (byte index)
		{
			if (index >= channels.Length)
				return null;

			return channels [index];
		}

		// TODO: Research if public is necessary
		/// <summary>
		/// Enqueues a message to the send queue.
		/// </summary>
		/// <param name="message"></param>
		public void EnqueueToSend (Message message)
		{
			if (isDisconnected)
				return;

			if (message.channel == null)
			{
				message.channel = GetMessageChannelByIndex (message.channelId);
				if (message.channel == null)
					return;
			}

			message.SetTime (stopwatch.ElapsedMilliseconds);

			lock (sendChannelLock)
			{
				message.channel.EnqueueToSend (message);
			}
		}

		// TODO: Research if public is necessary
		/// <summary>
		/// Dequeues a message from the send queue.
		/// </summary>
		/// <returns></returns>
		public Message DequeueFromSend ()
		{
			if (isDisconnected)
				return null;

			Message message = null;

			lock (sendChannelLock)
			{
				message = channels [currentSendChannelIndex].DequeueFromSend ();
				currentSendChannelIndex += 1;

				if (currentSendChannelIndex >= channels.Length)
					currentSendChannelIndex = 0;
			}

			if (message != null)
				return message;

			return null;
		}

		// TODO: Research if public is necessary
		/// <summary>
		/// Enqueues a message to the receive queue.
		/// </summary>
		/// <param name="message"></param>
		public void EnqueueToReceive (Message message)
		{
			if (isDisconnected)
				return;

			if (message.channel == null)
			{
				message.channel = GetMessageChannelByIndex (message.channelId);
				if (message.channel == null)
					return;
			}

			message.SetTime (stopwatch.ElapsedMilliseconds);
			latestPacketReceivedTimestamp = stopwatch.ElapsedMilliseconds;

			lock (receiveChannelLock)
			{
				message.channel.EnqueueToReceive (message);
			}
		}

		// TODO: Research if public is necessary
		/// <summary>
		/// Dequeues a message from the receive queue.
		/// </summary>
		/// <returns></returns>
		public Message DequeueFromReceive ()
		{
			if (isDisconnected)
				return null;

			Message message = null;

			lock (receiveChannelLock)
			{
				message = channels [currentReceiveChannelIndex].DequeueFromReceive ();
				currentReceiveChannelIndex += 1;

				if (currentReceiveChannelIndex >= channels.Length)
					currentReceiveChannelIndex = 0;
			}

			if (message != null)
				return message;

			return null;
		}

		/// <summary>
		/// Sends a connection message.
		/// </summary>
		private void SendConnectionMessage ()
		{
			if (isDisconnected)
				return;

			Message message = new Message (this, connectionHandler, 0, 0);
			EnqueueToSend (message);
		}

		/// <summary>
		/// Sends a keep alive message.
		/// </summary>
		private void SendKeepAliveMessage ()
		{
			if (isDisconnected)
				return;

			Message message = new Message (this, keepAliveHandler, 0, 10);

			lock (keepAlivesLock)
			{
				message.encoder.Encode (currentKeepAliveId);
				message.encoder.Encode (false);

				KeepAlive keepAlive = new KeepAlive ();
				keepAlive.id = currentKeepAliveId;
				keepAlive.timestamp = stopwatch.ElapsedMilliseconds;

				keepAlives.Push (keepAlive);
			}

			// TODO: Once overloaded, it should wrap around
			currentKeepAliveId += 1;

			EnqueueToSend (message);
		}

		/// <summary>
		/// Sends a disconnection message
		/// </summary>
		private void SendDisconnectionMessage ()
		{
			Message message = new Message (this, disconnectionHandler, 0, 1);

			// TODO: 0 is the error code
			message.encoder.Encode ((byte)0);

			messageManager.Send (message);

			// Send the same message to this host to trigger the OnDisconnect event.
			message = new Message (message);
			EnqueueToReceive (message);
		}

		/// <summary>
		/// Sets the connection as successful. This is only called from the ConnectionManager.
		/// </summary>
		internal void SetAsSuccessful ()
		{
			isSuccessful = true;
			SendConnectionMessage ();
		}

		/// <summary>
		/// Sets the disconnection event as called. This is only called from the ConnectionManager.
		/// </summary>
		internal void SetDisconnectEventAsCalled ()
		{
			disconnectEventCalled = true;
		}

		/// <summary>
		/// This should be called after a keep alive message has been ping-ponged (sent and received back).
		/// </summary>
		/// <param name="id">The id of the keep-alive.</param>
		internal void HandleKeepAlive (int id)
		{
			if (isDisconnected || !isSuccessful)
				return;

			lock (keepAlivesLock)
			{
				for (int i = 0; i < keepAlives.Length; i++)
				{
					if (keepAlives [-i].id != id)
						continue;

					latestPing = (int)(stopwatch.ElapsedMilliseconds - keepAlives [-i].timestamp);
					averagePing = (averagePing + latestPing) / 2;

					latestPacketReceivedTimestamp = stopwatch.ElapsedMilliseconds;

					break;
				}
			}
		}

		// TODO: Must not be public
		/// <summary>
		/// This method is called from a thread in the connection manager!
		/// </summary>
		internal void Threaded_Update ()
		{
			if (isDisconnected)
				return;

			Message message = null;
			message = DequeueFromSend ();
			if (message != null)
				messageManager.Send (message);

			if (!isSuccessful)
			{
				if (stopwatch.ElapsedMilliseconds - latestPacketReceivedTimestamp > ConnectTimeout)
					throw new TimeoutException ("Failed to connect to " + endPoint.ToString ());

				if (stopwatch.ElapsedMilliseconds - currentKeepAliveTimestamp < Frequency)
					return;

				SendConnectionMessage ();
				currentKeepAliveTimestamp = stopwatch.ElapsedMilliseconds;

				return;
			}

			if (stopwatch.ElapsedMilliseconds - latestPacketReceivedTimestamp > DisconnectTimeout)
				throw new TimeoutException ("No packets received for too long.");

			if (currentKeepAliveTimestamp + Frequency > stopwatch.ElapsedMilliseconds)
				return;

			SendKeepAliveMessage ();
			currentKeepAliveTimestamp = (int)stopwatch.ElapsedMilliseconds;
		}
	}
}
