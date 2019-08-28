using System.Net;

namespace ClassDev.Networking.Transport
{
	public class Message : LowLevel.Message
	{
		/// <summary>
		/// The channel id.
		/// </summary>
		public readonly byte channelId = 0;

		/// <summary>
		/// The network time in milliseconds when the message has been created.
		/// </summary>
		public long time { get; private set; }

		/// <summary>
		/// The channel used to send/receive this message.
		/// </summary>
		public MessageChannel channel = null;
		/// <summary>
		/// The message's sender/receiver connection.
		/// </summary>
		public Connection connection = null;

		/// <summary>
		/// Use this constructor if the message is to be sent.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="channel"></param>
		/// <param name="bufferSize"></param>
		public Message (Connection connection, MessageHandler handler, byte channelId, int bufferSize = BufferSize) : base (connection.endPoint, bufferSize + 5)
		{
			ValidateHandler (handler);

			this.connection = connection;

			encoder.Encode (channelId);

			channel = connection.GetMessageChannelByIndex (channelId);
			if (channel == null)
				throw new System.ArgumentNullException ("channelId", "The specified channel by id doesn't exist.");

			encoder.position += channel.GetRequiredBufferOffset ();

			encoder.Encode (handler.signature);
		}

		/// <summary>
		/// Use this constructor if the message is to be sent.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="bufferSize"></param>
		public Message (IPEndPoint endPoint, MessageHandler handler, int bufferSize = BufferSize) : base (endPoint, bufferSize + 5)
		{
			ValidateHandler (handler);

			encoder.Encode ((byte)0);
			encoder.Encode (handler.signature);
		}

		/// <summary>
		/// Use this constructor if the message is received.
		/// </summary>
		/// <param name="lowLevelMessage"></param>
		public Message (LowLevel.Message lowLevelMessage) : base (lowLevelMessage.endPoint, lowLevelMessage.buffer)
		{
			try
			{
				encoder.Decode (out channelId);
			}
			catch (System.Exception)
			{
				channelId = 0;
			}
		}

		/// <summary>
		/// Set the message timestamp.
		/// </summary>
		/// <param name="time"></param>
		public void SetTime (long time)
		{
			this.time = time;
		}

		/// <summary>
		/// Throws necessary exceptions if the handler is invalid.
		/// </summary>
		/// <param name="handler"></param>
		private void ValidateHandler (MessageHandler handler)
		{
			if (handler == null)
				throw new System.ArgumentNullException ("handler", "The message cannot be created without a message handler. The handler is required, so the receiver knows how to understand the message.");

			if (handler.signature == null)
				throw new System.ArgumentException ("handler", "The handler you provided is either not optimized with Optimize(), or is only used as a branch for other handlers. Branch handlers cannot understand messages.");
		}
	}
}
