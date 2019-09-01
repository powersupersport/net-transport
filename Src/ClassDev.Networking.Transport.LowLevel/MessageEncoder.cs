using System.IO;

namespace ClassDev.Networking.Transport
{
	public class MessageEncoder
	{
		private byte [] buffer = null;
		private MemoryStream stream = null;
		private BinaryReader reader = null;
		private BinaryWriter writer = null;

		public long position
		{
			get => stream.Position;
			set => stream.Position = value;
		}

		public MessageEncoder (int bufferSize) : this (new byte [bufferSize])
		{

		}
		public MessageEncoder (byte [] buffer)
		{
			this.buffer = buffer;
			stream = new MemoryStream (buffer);
			reader = new BinaryReader (stream);
			writer = new BinaryWriter (stream);
		}

		public void Reset ()
		{
			stream.Position = 0;
		}

		public void ImportData (byte [] data)
		{
			Reset ();
			Encode (data);
			Reset ();
		}

		public byte [] ExtractData ()
		{
			byte [] data = new byte [stream.Position];

			for (int i = 0; i < stream.Position; i++)
				data [i] = buffer [i];

			Reset ();

			return data;
		}

		public void SetPosition (int position)
		{
			stream.Position = position;
		}

		public override string ToString ()
		{
			return string.Join (",", buffer);
		}

		// =============================================================================

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (byte value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out byte value)
		{
			value = reader.ReadByte ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (byte [] values)
		{
			writer.Write (values);
		}

		// TODO: Fix this
		public void Decode (out byte [] values, int count = 0)
		{
			values = reader.ReadBytes (count);
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (sbyte value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out sbyte value)
		{
			value = reader.ReadSByte ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (bool value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out bool value)
		{
			value = reader.ReadBoolean ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [2 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (short value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [2 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out short value)
		{
			value = reader.ReadInt16 ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [2 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (ushort value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [2 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out ushort value)
		{
			value = reader.ReadUInt16 ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [4 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (int value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [4 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out int value)
		{
			value = reader.ReadInt32 ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [4 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (uint value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [4 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out uint value)
		{
			value = reader.ReadUInt32 ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [8 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (long value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [8 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out long value)
		{
			value = reader.ReadInt64 ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [8 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (ulong value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [8 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out ulong value)
		{
			value = reader.ReadUInt64 ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [4 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (float value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [4 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out float value)
		{
			value = reader.ReadSingle ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [8 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (double value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [8 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out double value)
		{
			value = reader.ReadDouble ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [16 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (decimal value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [16 bytes]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out decimal value)
		{
			value = reader.ReadDecimal ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Encode (char value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// [1 byte]
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out char value)
		{
			value = reader.ReadChar ();
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// [1 byte per each]
		/// </summary>
		/// <param name="values"></param>
		public void Encode (char [] values)
		{
			writer.Write (values);
		}

		/// <summary>
		/// [1 byte per each]
		/// </summary>
		/// <param name="values"></param>
		/// <param name="count"></param>
		public void Decode (out char [] values, int count = 0)
		{
			values = reader.ReadChars (count);
		}

		// -----------------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void Encode (string value)
		{
			writer.Write (value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void Decode (out string value)
		{
			value = reader.ReadString ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (IEncodable encodable)
		{
			encodable.Encode (this);
		}

		public void Decode (IEncodable encodable)
		{
			encodable.Decode (this);
		}
	}
}
