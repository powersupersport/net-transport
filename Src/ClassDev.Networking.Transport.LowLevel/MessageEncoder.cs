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

		// =============================================================================

		// -----------------------------------------------------------------------------

		public void Encode (byte value)
		{
			writer.Write (value);
		}

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

		public void Encode (sbyte value)
		{
			writer.Write (value);
		}

		public void Decode (out sbyte value)
		{
			value = reader.ReadSByte ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (bool value)
		{
			writer.Write (value);
		}

		public void Decode (out bool value)
		{
			value = reader.ReadBoolean ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (short value)
		{
			writer.Write (value);
		}

		public void Decode (out short value)
		{
			value = reader.ReadInt16 ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (ushort value)
		{
			writer.Write (value);
		}

		public void Decode (out ushort value)
		{
			value = reader.ReadUInt16 ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (int value)
		{
			writer.Write (value);
		}

		public void Decode (out int value)
		{
			value = reader.ReadInt32 ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (uint value)
		{
			writer.Write (value);
		}

		public void Decode (out uint value)
		{
			value = reader.ReadUInt32 ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (long value)
		{
			writer.Write (value);
		}

		public void Decode (out long value)
		{
			value = reader.ReadInt64 ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (ulong value)
		{
			writer.Write (value);
		}

		public void Decode (out ulong value)
		{
			value = reader.ReadUInt64 ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (float value)
		{
			writer.Write (value);
		}

		public void Decode (out float value)
		{
			value = reader.ReadSingle ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (double value)
		{
			writer.Write (value);
		}

		public void Decode (out double value)
		{
			value = reader.ReadDouble ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (decimal value)
		{
			writer.Write (value);
		}

		public void Decode (out decimal value)
		{
			value = reader.ReadDecimal ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (char value)
		{
			writer.Write (value);
		}

		public void Decode (out char value)
		{
			value = reader.ReadChar ();
		}

		// -----------------------------------------------------------------------------

		public void Encode (char [] values)
		{
			writer.Write (values);
		}

		public void Decode (out char [] values, int count)
		{
			values = reader.ReadChars (count);
		}

		// -----------------------------------------------------------------------------

		public void Encode (string value)
		{
			writer.Write (value);
		}

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
