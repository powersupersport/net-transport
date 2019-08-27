﻿using ClassDev.Networking.Transport.LowLevel;
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
		/// 
		/// </summary>
		public bool isSuccessful = false;
		/// <summary>
		/// 
		/// </summary>
		public bool isDisconnected = false;

		/// <summary>
		/// 
		/// </summary>
		private MessageManager messageManager;
		/// <summary>
		/// 
		/// </summary>
		public MessageChannel [] channels;
		/// <summary>
		/// 
		/// </summary>
		public IPEndPoint endPoint = null;

		/// <summary>
		/// 
		/// </summary>
		private MessageHandler connectionHandler;
		/// <summary>
		/// 
		/// </summary>
		private MessageHandler keepAliveHandler;
		// TODO: Shouldn't be public
		/// <summary>
		/// 
		/// </summary>
		public MessageHandler acknowledgementHandler;
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
			public int time;
		}
		/// <summary>
		/// 
		/// </summary>
		private CircularArray<KeepAlive> keepAlives = new CircularArray<KeepAlive> (50);
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

		// TODO: Could be moved into the host or connection manager instead.
		/// <summary>
		/// 
		/// </summary>
		private Stopwatch stopwatch;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="messageManager"></param>
		/// <param name="endPoint"></param>
		public Connection (ConnectionManager connectionManager, MessageChannelTemplate [] channelTemplates, IPEndPoint endPoint)
		{
			messageManager = connectionManager.messageManager;
			connectionHandler = connectionManager.messageHandler.connectionHandler;
			keepAliveHandler = connectionManager.messageHandler.keepAliveHandler;
			disconnectionHandler = connectionManager.messageHandler.disconnectionHandler;
			acknowledgementHandler = connectionManager.messageHandler.acknowledgementHandler;

			stopwatch = connectionManager.stopwatch;

			if (channelTemplates == null)
				channelTemplates = new MessageChannelTemplate [0];

			channels = new MessageChannel [channelTemplates.Length + 1];
			channels [0] = new MessageChannel (0);

			for (int i = 0; i < channelTemplates.Length; i++)
			{
				if (channelTemplates [i].isReliable)
					channels [i + 1] = new ReliableMessageChannel ((byte)(i + 1), this, messageManager, acknowledgementHandler, stopwatch, channelTemplates [i].isSequenced);
				else
					channels [i + 1] = new MessageChannel ((byte)(i + 1), channelTemplates [i].isSequenced);
			}

			this.endPoint = endPoint;
		}

		/// <summary>
		/// This method is called from a thread in the connection manager!
		/// </summary>
		public void Update ()
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

		/// <summary>
		/// 
		/// </summary>
		public void Disconnect ()
		{
			isDisconnected = true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void EnqueueToSend (Message message)
		{
			if (message.channel == null)
			{
				message.channel = GetMessageChannelByIndex (message.channelId);
				if (message.channel == null)
					return;
			}

			message.channel.EnqueueToSend (message);
		}

		// TODO: Drop unreliable messages if the buffer is overloaded

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Message DequeueFromSend ()
		{
			Message message = null;
			// TODO: The channel index should be an incremental value.
			// This is because if the first channel has lots of messages, the other channels will get delayed significantly.
			for (int i = 0; i < channels.Length; i++)
			{
				message = channels [i].DequeueFromSend ();
				if (message != null)
					return message;
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void EnqueueToReceive (Message message)
		{
			if (message.channel == null)
			{
				message.channel = GetMessageChannelByIndex (message.channelId);
				if (message.channel == null)
					return;
			}

			message.channel.EnqueueToReceive (message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Message DequeueFromReceive ()
		{
			Message message = null;
			// TODO: The channel index should be an incremental value.
			// This is because if the first channel has lots of messages, the other channels will get delayed significantly.
			for (int i = 0; i < channels.Length; i++)
			{
				message = channels [i].DequeueFromReceive ();
				if (message != null)
					return message;
			}

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
			messageManager.Send (message);
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

			messageManager.Send (message);

			KeepAlive keepAlive = new KeepAlive ();
			keepAlive.id = currentKeepAliveId;
			// TODO: Once the int is overloaded, it should wrap around.
			keepAlive.time = (int)stopwatch.ElapsedMilliseconds;

			keepAlives.Push (keepAlive);

			currentKeepAliveId += 1;
		}

		/// <summary>
		/// This should be called after a keep alive message has been ping-ponged (sent and received back).
		/// </summary>
		/// <param name="message"></param>
		public void HandleKeepAlive (int id)
		{
			if (!isSuccessful)
				isSuccessful = true;

			// TODO: Ping calculation seems to stop if the message buffers are overloaded.

			for (int i = 0; i < keepAlives.Length; i++)
			{
				if (keepAlives [i].id != id)
					continue;

				latestPing = (int)stopwatch.ElapsedMilliseconds - keepAlives [i].time;
				averagePing = (averagePing + latestPing) / 2;

				break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void SendDisconnectionMessage ()
		{

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
	}
}
