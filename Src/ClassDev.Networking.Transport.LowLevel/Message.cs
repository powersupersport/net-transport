using System.Net;

namespace ClassDev.Networking.Transport.LowLevel
{
	public class Message
	{
		/// <summary>
		/// The default new message size.
		/// </summary>
		public const int BufferSize = 1024;

		/// <summary>
		/// The IP endpoint of the sender.
		/// </summary>
		public IPEndPoint endPoint { get; set; }
		/// <summary>
		/// The message content.
		/// </summary>
		public byte [] buffer { get; private set; }
		/// <summary>
		/// The encoder used for the message content.
		/// </summary>
		public MessageEncoder encoder { get; private set; }

		/// <summary>
		/// Use this constructor if the message is to be sent.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="bufferSize">Use as small as possible. Perfect scenario would be if you already know how many bytes you're going to send.</param>
		/// <param name="channel">Check the ChannelCodes class for more info.</param>
		public Message (IPEndPoint endPoint, int bufferSize = BufferSize)
		{
			this.endPoint = endPoint;
			buffer = new byte [bufferSize];
			encoder = new MessageEncoder (buffer);
		}
		/// <summary>
		/// Use this constructor if the message is received (buffer is the received message).
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="data"></param>
		public Message (IPEndPoint endPoint, byte [] data)
		{
			this.endPoint = endPoint;
			buffer = data;
			encoder = new MessageEncoder (buffer);
		}

		/// <summary>
		/// Encode an encodable object.
		/// </summary>
		/// <param name="encodable"></param>
		public void Encode (IEncodable encodable)
		{
			encodable.Encode (encoder);
		}

		/// <summary>
		/// Decode an encodable object.
		/// </summary>
		/// <param name="encodable"></param>
		public void Decode (IEncodable encodable)
		{
			encodable.Decode (encoder);
		}

		/// <summary>
		/// Convert the message to string byte array.
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return string.Join (",", buffer);
		}
	}
}
