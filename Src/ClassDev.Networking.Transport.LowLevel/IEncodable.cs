namespace ClassDev.Networking.Transport
{
	/// <summary>
	/// Used to turn values or objects into binary data (byte arrays), so they can be transferred over the network.
	/// </summary>
	public interface IEncodable
	{
		/// <summary>
		/// Used to encode the type in bytes.
		/// </summary>
		/// <param name="messageEncoder">The encoder to be used.</param>
		void Encode (MessageEncoder messageEncoder);

		/// <summary>
		/// Used to decode the type from values.
		/// </summary>
		/// <param name="messageEncoder">The encoder to be used.</param>
		void Decode (MessageEncoder messageEncoder);
	}
}