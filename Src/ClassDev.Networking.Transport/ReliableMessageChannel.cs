using System.Collections.Generic;
using System.Diagnostics;

namespace ClassDev.Networking.Transport
{
	public class ReliableMessageChannel : MessageChannel
	{
		public const int ReliableResend = 300;

		public const int MaxQueuedMessages = 128;

		class ReliableCopy
		{
			public int sequenceIndex;
			public long time;
			public Message message;

			public ReliableCopy (Message message, int sequenceIndex, long time)
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
		private readonly object sentReliableCopiesLock = new object ();
		// TODO: If overloaded, should disconnect immediately.
		/// <summary>
		/// 
		/// </summary>
		private CircularArray<ReliableCopy> receivedReliableCopies;

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
		public ReliableMessageChannel (Connection connection, LowLevel.MessageManager messageManager, MessageHandler acknowledgementHandler, Stopwatch stopwatch, byte id, bool isSequenced = false) : base (connection, id, true, isSequenced)
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
			Message message = null;

			lock (sentReliableCopiesLock)
			{
				if (sentReliableCopies.Count > MaxQueuedMessages)
					throw new TimeoutException ("Reliable buffer was overloaded.");

				for (int i = 0; i < sentReliableCopies.Count; i++)
				{
					if (stopwatch.ElapsedMilliseconds - sentReliableCopies [i].message.time > Connection.DisconnectTimeout)
						throw new TimeoutException ("A reliable message took too long to acknowledge.");

					if (stopwatch.ElapsedMilliseconds - sentReliableCopies [i].time < ReliableResend)
						continue;

					sentReliableCopies [i].time = stopwatch.ElapsedMilliseconds;
					message = sentReliableCopies [i].message;
					break;
				}
			}

			if (message != null)
				return message;

			if (sendQueue.Count <= 0)
				return null;

			message = sendQueue.Dequeue ();
			EncodeSequenceIndexInMessage (message);

			lock (sentReliableCopiesLock)
			{
				sentReliableCopies.Add (new ReliableCopy (message, sendSequenceIndex, stopwatch.ElapsedMilliseconds));
			}

			sendSequenceIndex += 1;

			return message;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public override void EnqueueToReceive (Message message)
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

			if (isSequenced && sequenceIndex <= receiveSequenceIndex)
			{
				SendAcknowledgement (sequenceIndex);
				return;
			}

			if (sequenceIndex <= receiveSequenceIndex - MaxQueuedMessages)
			{
				throw new TimeoutException ("Too old packets received on a reliable unsequenced channel!");
			}

			if (sequenceIndex - receiveSequenceIndex >= MaxQueuedMessages)
			{
				receiveSequenceIndex += MaxQueuedMessages;
			}

			// TODO: Check if the queue is overloaded.

			if (receivedReliableCopies [sequenceIndex] == null || receivedReliableCopies [sequenceIndex].sequenceIndex < sequenceIndex - MaxQueuedMessages)
			{
				if (isSequenced)
				{
					if (sequenceIndex > receiveSequenceIndex + 1)
					{
						receivedReliableCopies [sequenceIndex] = new ReliableCopy (message, sequenceIndex, (int)stopwatch.ElapsedMilliseconds);
					}
					else
					{
						receiveQueue.Enqueue (message);

						receiveSequenceIndex = sequenceIndex;

						while (receivedReliableCopies [receiveSequenceIndex + 1] != null && receivedReliableCopies [receiveSequenceIndex + 1].message != null)
						{
							receiveSequenceIndex += 1;
							receiveQueue.Enqueue (receivedReliableCopies [receiveSequenceIndex].message);
							receivedReliableCopies [receiveSequenceIndex].message = null;
							receivedReliableCopies [receiveSequenceIndex] = null;
						}
					}
				}
				else
				{
					receivedReliableCopies [sequenceIndex] = new ReliableCopy (null, sequenceIndex, (int)stopwatch.ElapsedMilliseconds);
					receiveQueue.Enqueue (message);
				}
			}

			SendAcknowledgement (sequenceIndex);
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
			lock (sentReliableCopiesLock)
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
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		private void SendAcknowledgement (int sequenceIndex)
		{
			Message message = new Message (connection, acknowledgementHandler, 0, 7);
			message.encoder.Encode (id);
			message.encoder.Encode (sequenceIndex);
			connection.EnqueueToSend (message);
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
