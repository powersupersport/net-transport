using ClassDev.Networking.Transport;
using System.Net;

namespace ClassDev.Networking
{
	public class NetworkManager
	{
		/// <summary>
		/// True if the network manager is working.
		/// </summary>
		public bool isStarted { get; private set; }

		/// <summary>
		/// True if the network manager is running as a server.
		/// </summary>
		public bool isServer { get; private set; }

		/// <summary>
		/// The transport layer host used for this network manager.
		/// </summary>
		public Host host { get; private set; }

		/// <summary>
		/// The port this network manager is running on.
		/// </summary>
		public int port { get; private set; }

		/// <summary>
		/// Start the network manager as a server on a random free port.
		/// </summary>
		public void StartServer (int maxConnections)
		{
			StartServer (0, maxConnections);
		}
		/// <summary>
		/// Start the network manager as a server on a specific port.
		/// </summary>
		/// <param name="port"></param>
		public void StartServer (int port, int maxConnections)
		{
			if (isStarted)
				throw new System.InvalidOperationException ("Trying to start the network manager whilst it's already started.");

			isStarted = true;
			isServer = true;

			host = new Host (port, maxConnections);
			host.Start ();
		}

		/// <summary>
		/// Start the network manager as a client.
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="port"></param>
		public void StartClient (string ipAddress, int port)
		{
			if (isStarted)
				throw new System.InvalidOperationException ("Trying to start the network manager whilst it's already started.");

			isServer = false;
			isStarted = true;

			host = new Host (port, 1);
			host.Start ();

			host.Connect (ipAddress, port);
		}

		/// <summary>
		/// Updates the necessary components. You should call this in your own synchronous update loop.
		/// </summary>
		public void Update ()
		{
			if (!isStarted)
				return;

			host.Update ();
		}

		/// <summary>
		/// Stop the network manager. Call this before closing the application.
		/// </summary>
		public void Stop ()
		{
			if (!isStarted)
				return;

			isStarted = false;
			isServer = false;

			host.Stop ();
			host = null;
		}

		// --------------------------------------------------------------------------------------------

		/// <summary>
		/// Creates a new host and starts listening for messages.
		/// </summary>
		private void SetupHost ()
		{
			
		}

		/// <summary>
		/// Stops the host and clears up memory.
		/// </summary>
		private void TeardownHost ()
		{

		}
	}
}
