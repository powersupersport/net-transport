using System.Net.Sockets;
using System;

namespace ClassDev.Networking.Transport
{
	public class Host
	{
		/// <summary>
		/// True if the host is running (sends and listens for messages)
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// The UDP client used for sending and receiving messages.
		/// </summary>
		public UdpClient udpClient;
		/// <summary>
		/// The message manager used for managing sending and receiving messages.
		/// </summary>
		public MessageManager messageManager;

		/// <summary>
		/// The port on which the UDP client is running.
		/// </summary>
		public int port { get; private set; }

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Host () : this (0)
		{

		}
		/// <summary>
		/// Constructor with port.
		/// </summary>
		public Host (int port)
		{
			this.port = port;
		}

		/// <summary>
		/// Start the host.
		/// </summary>
		public void Start ()
		{
			isStarted = true;

			SetupUdpClient ();

			SetupMessageManager ();
		}

		/// <summary>
		/// Stop the host.
		/// </summary>
		public void Stop ()
		{
			isStarted = false;

			ShutdownMessageManager ();

			ShutdownUdpClient ();
		}

		// ---------------------------------------------------------------------------

		/// <summary>
		/// Initializes the UDP client.
		/// </summary>
		private void SetupUdpClient ()
		{
			if (udpClient != null)
				return;

			udpClient = new UdpClient (port);

			// TODO: Has to be changeable from the settings.
			udpClient.Client.ReceiveTimeout = 50;
		}

		/// <summary>
		/// Closes the UDP client port and disposes the object.
		/// </summary>
		private void ShutdownUdpClient ()
		{
			if (udpClient == null)
				return;

			udpClient.Close ();
			udpClient.Dispose ();
			udpClient = null;
		}

		/// <summary>
		/// Initializes the message manager.
		/// </summary>
		private void SetupMessageManager ()
		{
			if (messageManager != null)
				ShutdownMessageManager ();

			if (udpClient == null)
				throw new Exception ("Cannot set up message manager if the UdpClient is null.");

			messageManager = new MessageManager (udpClient);
			messageManager.Start ();
		}

		/// <summary>
		/// Stops the message manager and resets the variable.
		/// </summary>
		private void ShutdownMessageManager ()
		{
			if (messageManager == null)
				return;

			messageManager.Stop ();
			messageManager = null;
		}
	}
}
