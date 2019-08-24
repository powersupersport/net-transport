using System.Collections.Generic;

namespace ClassDev.Networking.Transport
{
	public class MessageChannel
	{
		public string name = "";
		public bool isReliable = false;
		public bool isSequenced = false;

		public Queue<Message> sendQueue;
		public Queue<Message> receiveQueue;

		public MessageChannel (bool isReliable = false, bool isSequenced = false)
		{
			this.isReliable = isReliable;
			this.isSequenced = isSequenced;

			sendQueue = new Queue<Message> ();
			receiveQueue = new Queue<Message> ();
		}

		public void EnqueueToSend (Message message)
		{
			if (isSequenced)
			{

			}

			sendQueue.Enqueue (message);
		}

		public Message DequeueFromSend ()
		{
			if (sendQueue.Count <= 0)
				return null;

			return sendQueue.Dequeue ();
		}

		public void EnqueueToReceive (Message message)
		{
			receiveQueue.Enqueue (message);
		}

		public Message DequeueFromReceive ()
		{
			if (receiveQueue.Count <= 0)
				return null;

			return receiveQueue.Dequeue ();
		}

		public int GetRequiredBufferOffset ()
		{
			if (isSequenced)
				return sizeof (int);

			return 0;
		}
	}
}
