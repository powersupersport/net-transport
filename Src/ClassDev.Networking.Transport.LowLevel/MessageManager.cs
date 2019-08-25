﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;

namespace ClassDev.Networking.Transport.LowLevel
{
	public class MessageManager
	{
		#region Setup

		/// <summary>
		/// The UDP client used for sending and receiving. This is assigned from the constructor.
		/// </summary>
		private UdpClient udpClient;

		/// <summary>
		/// True if the message manager is running.
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public MessageManager (UdpClient udpClient)
		{
			this.udpClient = udpClient;
		}

		/// <summary>
		/// Starts the message manager.
		/// </summary>
		public void Start ()
		{
			if (isStarted)
				return;

			if (udpClient == null)
				throw new Exception ("Message manager cannot be started if the UdpClient is null.");

			isStarted = true;

			receiveThread = new Thread (Threaded_ReceiveMessages);
			receiveThread.Start ();

			sendThread = new Thread (Threaded_SendMessages);
			sendThread.Start ();
		}

		/// <summary>
		/// Stops the message manager.
		/// </summary>
		public void Stop ()
		{
			if (!isStarted)
				return;

			isStarted = false;

			if (sendThread.IsAlive)
				sendThread.Join ();

			if (receiveThread.IsAlive)
				receiveThread.Join ();

			sendThread = null;
			receiveThread = null;
		}

		#endregion

		#region Sending/Receiving

		/// <summary>
		/// Thread for message sending.
		/// </summary>
		private Thread sendThread;
		/// <summary>
		/// Thread for message receiving.
		/// </summary>
		private Thread receiveThread;

		/// <summary>
		/// Messages to send.
		/// </summary>
		Queue<Message> sendQueue = new Queue<Message> ();
		/// <summary>
		/// Messages to receive.
		/// </summary>
		Queue<Message> receiveQueue = new Queue<Message> ();

		/// <summary>
		/// Sends a message.
		/// </summary>
		public void Send (IPEndPoint endPoint, byte [] message)
		{
			Send (new Message (endPoint, message));
		}
		/// <summary>
		/// Sends a message.
		/// </summary>
		/// <param name="message"></param>
		public void Send (Message message)
		{
			if (message == null)
				throw new ArgumentNullException ("The provided message to send is null.");

			sendQueue.Enqueue (message);
		}

		/// <summary>
		/// Receives a message from the queue. If there are none, null will be returned.
		/// </summary>
		public Message Receive ()
		{
			if (receiveQueue.Count <= 0)
				return null;

			return receiveQueue.Dequeue ();
		}

		/// <summary>
		/// Sends messages.
		/// </summary>
		private void Threaded_SendMessages ()
		{
			Message message = null;

			while (true)
			{
				if (!isStarted)
					return;

				while (sendQueue.Count > 0)
				{
					message = sendQueue.Dequeue ();
					udpClient.Send (message.buffer, (int)message.encoder.position, message.endPoint);
				}
			}
		}

		/// <summary>
		/// Receives messages.
		/// </summary>
		private void Threaded_ReceiveMessages ()
		{
			Message message = null;
			IPEndPoint endPoint = new IPEndPoint (IPAddress.Any, 0);
			byte [] messageContent = null;

			while (true)
			{
				if (!isStarted)
					return;

				try
				{
					messageContent = udpClient.Receive (ref endPoint);
					message = new Message (endPoint, messageContent);
					receiveQueue.Enqueue (message);
				}
				catch (SocketException)
				{
					
				}
			}
		}

		#endregion
	}
}