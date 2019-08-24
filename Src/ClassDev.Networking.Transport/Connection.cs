using System.Diagnostics;
using System.Net;
using ClassDev.Networking.Transport.LowLevel;

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
		private MessageHandler handler;
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
		public int ping = 0;

		/// <summary>
		/// 
		/// </summary>
		private Stopwatch stopwatch;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="messageManager"></param>
		/// <param name="endPoint"></param>
		public Connection (MessageManager messageManager, MessageHandler handler, IPEndPoint endPoint)
		{
			this.messageManager = messageManager;

			channels = new MessageChannel [1];
			channels [0] = new MessageChannel ();

			this.handler = handler;
			this.endPoint = endPoint;

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

			if (stopwatch.ElapsedMilliseconds > Frequency)
			{
				SendKeepAliveMessage ();
				stopwatch.Restart ();
			}
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
		private void SendKeepAliveMessage ()
		{
			if (isDisconnected)
				return;

			Message message = new Message (this, handler, 0, 10);

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
	}
}
