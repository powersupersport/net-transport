using ClassDev.Networking.Transport.LowLevel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace ClassDev.Networking.Transport
{
	public class Connection
	{
		/// <summary>
		/// 
		/// </summary>
		public const int Timeout = 10000;
		/// <summary>
		/// 
		/// </summary>
		public const int Frequency = 200;

		/// <summary>
		/// The connection identifier.
		/// </summary>
		public int id { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public bool isSuccessful = false;
		/// <summary>
		/// 
		/// </summary>
		public bool isDisconnected = false;

		/// <summary>
		/// The time when the disconnection occurrs.
		/// </summary>
		public long disconnectionTime { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		private MessageManager messageManager;
		/// <summary>
		/// 
		/// </summary>
		public IPEndPoint endPoint = null;

		/// <summary>
		/// 
		/// </summary>
		public MessageChannel [] channels;
		/// <summary>
		/// 
		/// </summary>
		private readonly object sendChannelLock = new object ();
		/// <summary>
		/// 
		/// </summary>
		private readonly object receiveChannelLock = new object ();

		/// <summary>
		/// 
		/// </summary>
		private MessageHandler connectionHandler;
		/// <summary>
		/// 
		/// </summary>
		private MessageHandler keepAliveHandler;
		/// <summary>
		/// 
		/// </summary>
		private MessageHandler acknowledgementHandler;
		/// <summary>
		/// 
		/// </summary>
		private MessageHandler disconnectionHandler;

		/// <summary>
		/// 
		/// </summary>
		private struct KeepAlive
		{
			public int id;
			public long time;
		}
		/// <summary>
		/// 
		/// </summary>
		private CircularArray<KeepAlive> keepAlives = new CircularArray<KeepAlive> (50);
		/// <summary>
		/// Thread lock for keep alive records.
		/// </summary>
		private readonly object keepAlivesLock = new object ();

		/// <summary>
		/// 
		/// </summary>
		private int currentKeepAliveId = 0;
		/// <summary>
		/// 
		/// </summary>
		private int currentKeepAliveTime = 0;

		/// <summary>
		/// 
		/// </summary>
		public int latestPing { get; private set; }
		/// <summary>
		/// 
		/// </summary>
		public int averagePing { get; private set; }
		/// <summary>
		/// 
		/// </summary>
		public int ping
		{
			get => averagePing;
		}

		/// <summary>
		/// 
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

		/// <summary>
		/// 
		/// </summary>
		public void Disconnect ()
		{
			if (isDisconnected)
				return;

			isDisconnected = true;
			disconnectionTime = stopwatch.ElapsedMilliseconds;

			for (int i = 0; i < 3; i++)
			{
				SendDisconnectionMessage ();
			}
		}

		/// <summary>
		/// 
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

		/// <summary>
		/// 
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
			}
			currentSendChannelIndex += 1;

			if (currentSendChannelIndex >= channels.Length)
				currentSendChannelIndex = 0;

			if (message != null)
				return message;

			return null;
		}

		/// <summary>
		/// 
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
			lock (receiveChannelLock)
			{
				message.channel.EnqueueToReceive (message);
			}
		}

		/// <summary>
		/// 
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
			}
			currentReceiveChannelIndex += 1;

			if (currentReceiveChannelIndex >= channels.Length)
				currentReceiveChannelIndex = 0;

			if (message != null)
				return message;

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		private void SendConnectionMessage ()
		{
			if (isDisconnected)
				return;

			Message message = new Message (this, connectionHandler, 0, 10);
			EnqueueToSend (message);
		}

		/// <summary>
		/// 
		/// </summary>
		private void SendKeepAliveMessage ()
		{
			if (isDisconnected)
				return;

			Message message = new Message (this, keepAliveHandler, 0, 10);
			message.encoder.Encode (currentKeepAliveId);
			message.encoder.Encode (false);

			EnqueueToSend (message);

			KeepAlive keepAlive = new KeepAlive ();
			keepAlive.id = currentKeepAliveId;
			keepAlive.time = stopwatch.ElapsedMilliseconds;

			lock (keepAlivesLock)
			{
				keepAlives.Push (keepAlive);
			}

			// TODO: Once overloaded, it should wrap around
			currentKeepAliveId += 1;
		}

		/// <summary>
		/// This should be called after a keep alive message has been ping-ponged (sent and received back).
		/// </summary>
		/// <param name="message"></param>
		public void HandleKeepAlive (int id)
		{
			if (isDisconnected)
				return;

			if (!isSuccessful)
				isSuccessful = true;

			// TODO: Ping calculation seems to stop if the message buffers are overloaded.

			lock (keepAlivesLock)
			{
				for (int i = 0; i < keepAlives.Length; i++)
				{
					if (keepAlives [i].id != id)
						continue;

					latestPing = (int)(stopwatch.ElapsedMilliseconds - keepAlives [i].time);
					averagePing = (averagePing + latestPing) / 2;

					break;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void SendDisconnectionMessage ()
		{
			Message message = new Message (this, disconnectionHandler, 0, 1);
			message.encoder.Encode (0);
			messageManager.Send (message);
		}

		/// <summary>
		/// Returns the channel instance with the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public MessageChannel GetMessageChannelByIndex (byte index)
		{
			if (index >= channels.Length)
				return null;

			return channels [index];
		}

		/// <summary>
		/// This method is called from a thread in the connection manager!
		/// </summary>
		public void Threaded_Update ()
		{
			if (isDisconnected)
				return;

			Message message = null;
			do
			{
				message = DequeueFromSend ();
				if (message != null)
					messageManager.Send (message);
			}
			while (message != null);

			if (!isSuccessful)
			{
				if (currentKeepAliveTime + Frequency > stopwatch.ElapsedMilliseconds)
					return;

				SendConnectionMessage ();
				currentKeepAliveTime = (int)stopwatch.ElapsedMilliseconds;
				return;
			}

			if (currentKeepAliveTime + Frequency > stopwatch.ElapsedMilliseconds)
				return;

			SendKeepAliveMessage ();
			currentKeepAliveTime = (int)stopwatch.ElapsedMilliseconds;
		}
	}
}
