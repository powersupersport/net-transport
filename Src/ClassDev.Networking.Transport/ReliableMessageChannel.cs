using System.Collections.Generic;
using System.Diagnostics;

namespace ClassDev.Networking.Transport
{
	public class ReliableMessageChannel : MessageChannel
	{
		class ReliableCopy
		{
			public int sequenceIndex;
			public int time;
			public Message message;

			public ReliableCopy (Message message, int sequenceIndex, int time)
			{
				this.message = message;
				this.sequenceIndex = sequenceIndex;
				this.time = time;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private List<ReliableCopy> sentReliableCopies;
		/// <summary>
		/// 
		/// </summary>
		private CircularArray<ReliableCopy> receivedReliableCopies;

		/// <summary>
		/// 
		/// </summary>
		private Connection connection;
		/// <summary>
		/// 
		/// </summary>
		private LowLevel.MessageManager messageManager;
		/// <summary>
		/// 
		/// </summary>
		private MessageHandler acknowledgementHandler;
		/// <summary>
		/// 
		/// </summary>
		private Stopwatch stopwatch;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="isSequenced"></param>
		public ReliableMessageChannel (byte id, Connection connection, LowLevel.MessageManager messageManager, MessageHandler acknowledgementHandler, Stopwatch stopwatch, bool isSequenced = false) : base (id, true, isSequenced)
		{
			sentReliableCopies = new List<ReliableCopy> ();
			receivedReliableCopies = new CircularArray<ReliableCopy> (128);

			this.connection = connection;
			this.messageManager = messageManager;
			this.acknowledgementHandler = acknowledgementHandler;
			this.stopwatch = stopwatch;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public override void EnqueueToSend (Message message)
		{
			sendQueue.Enqueue (message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public override Message DequeueFromSend ()
		{
			for (int i = 0; i < sentReliableCopies.Count; i++)
			{
				if ((int)stopwatch.ElapsedMilliseconds - sentReliableCopies [i].time < ReliableResend)
					continue;

				sentReliableCopies [i].time = (int)stopwatch.ElapsedMilliseconds;
				return sentReliableCopies [i].message;
			}

			if (sendQueue.Count <= 0)
				return null;

			Message message = sendQueue.Dequeue ();
			EncodeSequenceIndexInMessage (message);
			sentReliableCopies.Add (new ReliableCopy (message, sendSequenceIndex, (int)stopwatch.ElapsedMilliseconds));
			sendSequenceIndex += 1;

			return message;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public override void EnqueueToReceive (Message message)
		{
			message.encoder.Decode (out int sequenceIndex);

			if (sequenceIndex <= receiveSequenceIndex - 64)
			{
				// TODO: Throw a timeout exception.
				return;
			}

			// TODO: Disconnect on so many overflows.

			int index = sequenceIndex - receiveSequenceIndex;

			if (index >= 128)
			{
				receiveSequenceIndex += 64;
				index -= 64;
			}

			// TODO: Check if the queue is overloaded.

			if (receivedReliableCopies [index] == null)
			{
				receivedReliableCopies [index] = new ReliableCopy (null, sequenceIndex, (int)stopwatch.ElapsedMilliseconds);
				receiveQueue.Enqueue (message);
			}

			SendAcknowledgement (connection, sequenceIndex);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override Message DequeueFromReceive ()
		{
			if (receiveQueue.Count <= 0)
				return null;

			return receiveQueue.Dequeue ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sequenceIndex"></param>
		public void Acknowledge (int sequenceIndex)
		{
			for (int i = 0; i < sentReliableCopies.Count; i++)
			{
				if (sentReliableCopies [i].sequenceIndex == sequenceIndex)
				{
					sentReliableCopies.RemoveAt (i);
					return;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		private void SendAcknowledgement (Connection connection, int sequenceIndex)
		{
			Message message = new Message (connection, acknowledgementHandler, 0, 7);
			message.encoder.Encode (id);
			message.encoder.Encode (sequenceIndex);
			messageManager.Send (message);
		}

		/// <summary>
		/// All reliable messages need an index.
		/// </summary>
		/// <returns></returns>
		public override int GetRequiredBufferOffset ()
		{
			return sizeof (int);
		}
	}
}
