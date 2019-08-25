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
		public Connection (MessageManager messageManager, BaseHandler handler, IPEndPoint endPoint)
		{
			this.messageManager = messageManager;

			channels = new MessageChannel [1];
			channels [0] = new MessageChannel ();

			this.endPoint = endPoint;

			connectionHandler = handler.connectionHandler;
			keepAliveHandler = handler.keepAliveHandler;
			disconnectionHandler = handler.disconnectionHandler;

			stopwatch = new Stopwatch ();
			stopwatch.Start ();
		}

		/// <summary>
		/// Called from the connection manager.
		/// </summary>
		public void Update ()
		{
			if (isDisconnected)
				return;

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
