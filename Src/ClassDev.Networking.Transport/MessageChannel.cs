using System.Collections.Generic;

namespace ClassDev.Networking.Transport
{
	public class MessageChannel
	{
		/// <summary>
		/// Just for info. Doesn't serve any function.
		/// </summary>
		public string name = "";
		/// <summary>
		/// Defines if the messages are reliable.
		/// </summary>
		public bool isReliable { get; private set; }
		/// <summary>
		/// Defines if the messages are going to be received in order.
		/// </summary>
		public bool isSequenced { get; private set; }
		/// <summary>
		/// The sequence index for outgoing messages.
		/// </summary>
		private int sendSequenceIndex = 1;
		/// <summary>
		/// The sequence index for incoming messages.
		/// </summary>
		public int receiveSequenceIndex = 0;

		/// <summary>
		/// The send queue.
		/// </summary>
		public Queue<Message> sendQueue;
		/// <summary>
		/// The receive queue.
		/// </summary>
		public Queue<Message> receiveQueue;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="template"></param>
		public MessageChannel (MessageChannelTemplate template) : this (template.isReliable, template.isSequenced)
		{

		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="isReliable"></param>
		/// <param name="isSequenced"></param>
		public MessageChannel (bool isReliable = false, bool isSequenced = false)
		{
			this.isReliable = isReliable;
			this.isSequenced = isSequenced;

			sendQueue = new Queue<Message> ();
			receiveQueue = new Queue<Message> ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void EnqueueToSend (Message message)
		{
			if (isSequenced)
			{
				// The position must be kept and restored, because it's the length identifier of the message.
				int currentPositon = (int)message.encoder.position;

				message.encoder.position = sizeof (byte);
				message.encoder.Encode (sendSequenceIndex);

				message.encoder.position = currentPositon;

				sendSequenceIndex += 1;
			}

			sendQueue.Enqueue (message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Message DequeueFromSend ()
		{
			if (sendQueue.Count <= 0)
				return null;

			return sendQueue.Dequeue ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void EnqueueToReceive (Message message)
		{
			if (isSequenced)
			{
				message.encoder.Decode (out int sequenceIndex);

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
		public Message DequeueFromReceive ()
		{
			if (receiveQueue.Count <= 0)
				return null;

			return receiveQueue.Dequeue ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int GetRequiredBufferOffset ()
		{
			if (isSequenced)
				return sizeof (int);

			return 0;
		}
	}
}
