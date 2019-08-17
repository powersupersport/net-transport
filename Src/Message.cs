using System.Net;

namespace ClassDev.Networking.Transport
{
	public class Message
	{
		/// <summary>
		/// The IP endpoint of the sender.
		/// </summary>
		public IPEndPoint endPoint;
		/// <summary>
		/// The message content.
		/// </summary>
		public byte [] content;

		public Message (IPEndPoint endPoint, byte [] content)
		{
			this.endPoint = endPoint;
			this.content = content;
		}
	}
}
