using System.Collections.Generic;

namespace ClassDev.Networking.Transport
{
	public class MessageChannel
	{
		public const int UnreliableDropTimeout = 500;

		/// <summary>
		/// The channel ID relative to the connection.
		/// </summary>
		public byte id { get; private set; }

		/// <summary>
		/// Just for info. Doesn't serve any function.
		/// </summary>
		public string name = "";
		/// <summary>
		/// Defines if the messages are reliable.
		/// </summary>
		public bool isReliable { get; protected set; }
		/// <summary>
		/// Defines if the messages are going to be received in order.
		/// </summary>
		public bool isSequenced { get; protected set; }
		/// <summary>
		/// The sequence index for outgoing messages.
		/// </summary>
		protected int sendSequenceIndex = 1;
		/// <summary>
		/// The sequence index for incoming messages.
		/// </summary>
		protected int receiveSequenceIndex = 0;

		/// <summary>
		/// The send queue.
		/// </summary>
		protected Queue<Message> sendQueue;
		/// <summary>
		/// The receive queue.
		/// </summary>
		protected Queue<Message> receiveQueue;

		/// <summary>
		/// 
		/// </summary>
		protected Connection connection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="isReliable"></param>
		/// <param name="isSequenced"></param>
		public MessageChannel (Connection connection, byte id, bool isSequenced = false) : this (connection, id, false, isSequenced)
		{

		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="isReliable"></param>
		/// <param name="isSequenced"></param>
		protected MessageChannel (Connection connection, byte id, bool isReliable, bool isSequenced)
		{
			this.connection = connection;

			this.id = id;
			this.isReliable = isReliable;
			this.isSequenced = isSequenced;

			sendQueue = new Queue<Message> ();
			receiveQueue = new Queue<Message> ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public virtual void EnqueueToSend (Message message)
		{
			if (isSequenced)
			{
				EncodeSequenceIndexInMessage (message);

				sendSequenceIndex += 1;
			}

			sendQueue.Enqueue (message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual Message DequeueFromSend ()
		{
			Message message = null;
			do
			{
				if (sendQueue.Count <= 0)
					return null;

				message = sendQueue.Dequeue ();
			}
			while (connection.stopwatch.ElapsedMilliseconds - message.time > UnreliableDropTimeout);

			return message;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public virtual void EnqueueToReceive (Message message)
		{
			if (isSequenced)
			{
				int sequenceIndex = 0;

				try
				{
					message.encoder.Decode (out sequenceIndex);
				}
				catch (System.Exception)
				{
					return;
				}

				if (sequenceIndex <= receiveSequenceIndex)
					return;

				receiveSequenceIndex = sequenceIndex;
			}

			receiveQueue.Enqueue (message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual Message DequeueFromReceive ()
		{
			Message message = null;

			do
			{
				if (receiveQueue.Count <= 0)
					return null;

				message = receiveQueue.Dequeue ();
			}
			while (connection.stopwatch.ElapsedMilliseconds - message.time > UnreliableDropTimeout);

			return message;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		protected void EncodeSequenceIndexInMessage (Message message)
		{
			// The position must be kept and restored, because it's the length identifier of the message.
			int currentPositon = (int)message.encoder.position;

			message.encoder.position = sizeof (byte);
			message.encoder.Encode (sendSequenceIndex);

			message.encoder.position = currentPositon;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual int GetRequiredBufferOffset ()
		{
			if (isSequenced)
				return sizeof (int);

			return 0;
		}
	}
}
