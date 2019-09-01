# MessageEncoder

The message encoder is a class that allows you to encode different types of data into a stream of bytes. It uses intuitive methods to simplify its usage.

For those interested, the message encoder actually just a wrapper around the ```System.IO.BinaryReader``` and ```System.IO.BinaryReader``` and utilizes ```System.IO.MemoryStream``` to manage the buffer. You can read more about those in the [MSDN Docs](https://docs.microsoft.com/en-us/dotnet/api/system.io).

Here is a basic example of how you'd use the MessageEncoder. Let's say you have this data to send/receive:

```csharp
int health = 90;
int points = 150;
bool isDead = true;
```

On the sending side you'll create a MessageEncoder and start writing this data as follows:

```csharp
messageEncoder = new MessageEncoder (32); // 32 is how big the buffer will be.

messageEncoder.Encode (health); // Will encode 4 bytes.
messageEncoder.Encode (points); // Will encode 4 bytes.
messageEncoder.Encode (isDead); // Will encode 1 byte.
```

This is how your data will look like and this is how it's going to be sent out and received on the other end:

```[ 90] [  0] [  0] [  0] [150] [  0] [  0] [  0] [  1]``` - 9 bytes long

Now let's see how we'd decode that on the other end. You must use the same pattern otherwise there's now way that it would work.

```csharp
messageEncoder = new MessageEncoder (32);

 // This is the byte array that will come with the received message.
messageEncoder.ImportData (data);

int health = 0;
int points = 0;
bool isDead = false;

messageEncoder.Decode (out health); // Will decode 4 bytes.
messageEncoder.Decode (out points); // Will decode 4 bytes.
messageEncoder.Decode (out isDead); // Will decode 1 byte.
```

And this is how you send and receive a message.

### Protect your app from attackers!

Now, let's say that a bad guy decides to send us this message:

```[ 90] [  0] [  0] [  0] [150] [  0] [  0]``` - 7 bytes long

We clearly expect to read 9 bytes in our code above. If this message is received, then an exception will be thrown as we're trying to read more bytes than there actually are, and our app will crash.

To protect against this, always use a ```try-catch```. If the message fails the try-catch, then you should discard (ignore) the message. Here's how you'd do that:

```csharp
try
{
    messageEncoder.Decode (out health); // Will decode 4 bytes.
    messageEncoder.Decode (out points); // Will decode 4 bytes.
    messageEncoder.Decode (out isDead); // Will decode 1 byte.
}
catch (System.Exception)
{
    // Do domething if the message fails.
    // In most cases, just return out of the method you use for decoding.
    return;
}
```

In that case you'll not get an exception if the message is trimmed.

Just remember: **Always use a try-catch when decoding a message!**

**An important note about cheating!**

Note that this doesn't protect against cheating though. If an attacker sends the expected message length, but with other values, then those values will be expected and decoded accordingly. In that case you'd have to look into validating the message data (ex. making sure that the player can actually have the specified health), as well as doing some research on **server authoritative design**.

&nbsp;

## Properties

### ```MessageEncoder.position```
```long position { get; set; }```

Gets or sets the position of the virtual caret.

Example. If your stream looks like this (The caret is the ```|``` sign), getting the value would return 4 (the next cell to write):

```[0] [1] [2] [3] | [ ] [ ] [ ]```

Whilst setting the value to 2 would do this:

```[0] [1] | [2] [3] [ ] [ ] [ ]```

You can use that to allocate empty cells or overwrite data if needed.

**It's important to note that the caret position determines the length of the message when sending. If you do modify it, you should restore it to its original position afterwards as your message will get cut off. As in the following example:**

Original with a modified caret:

```[0] [1] [2] [3] | [4] [5] [ ]```

What is going to be sent out:

```[0] [1] [2] [3]```

&nbsp;

## Methods

### ```MessageEncoder ()```
Constructor for MessageEncoder.

```MessageEncoder (byte [] buffer)```

- Parameters:
	- ```buffer``` - An array of bytes to be used for encoding/decoding.

### ```MessageEncoder.Reset ()```
Resets the memory caret (essentially an alias for ```MessageEncoder.position = 0```). Note that this doesn't actually delete any data written in the buffer for performance reasons.

```void Reset ()```

### ```MessageEncoder.ImportData ()```
Imports a byte array into the buffer.

```void ImportData (byte [] data)```

- Parameters:
	- ```data``` - The array of data to read.

### ```MessageEncoder.ExtractData ()```
Returns a trimmed array of the written data. Use this only if necessary.

```byte [] ExtractData ()```

- Returns:
	- ```byte []``` - The byte array with the data.

### ```MessageEncoder.SetPosition ()```
An alias for ```MessageManager.position```. Sets the position of the caret.

```void SetPosition (int position)```

### ```MessageEncoder.ToString ()```
Returns an array representation of the buffer as a string.

```string ToString ()```

- Returns:
	- ```string``` - The array representation string.

### ```MessageEncoder.Encode ()```
Use any of these methods to encode data types.

```void Encode (byte value)```

Encodes a single byte.

- Parameters:
	- ```value``` - The byte to encode.

```void Encode (byte [] values)```

Encodes multiple bytes.

- Parameters:
	- ```values``` - An array of bytes to encode.

```void Encode (sbyte value)```

Encodes a single signed byte.

- Parameters:
	- ```value``` - The sbyte to encode.

```void Encode (bool value)```

Encodes a boolean.

- Parameters:
	- ```value``` - The bool to encode.

```void Encode (short value)```

Encodes a short signed integer.

- Parameters:
	- ```value``` - The short to encode.

```void Encode (ushort value)```

Encodes an unsigned short integer.

- Parameters:
	- ```value``` - The ushort to encode.

```void Encode (int value)```

Encodes a traditional signed integer.

- Parameters:
	- ```value``` - The int to encode.

```void Encode (uint value)```

Encodes an unsigned integer.

- Parameters:
	- ```value``` - The uint to encode.

```void Encode (long value)```

Encodes a long signed int.

- Parameters:
	- ```value``` - The long to encode.

```void Encode (ulong value)```

Encodes an unsigned long int.

- Parameters:
	- ```value``` - The ulong to encode.

```void Encode (float value)```

Encodes a float.

- Parameters:
	- ```value``` - The float to encode to encode.

```void Encode (double value)```

Encodes a double.

- Parameters:
	- ```value``` - The double to encode.

```void Encode (decimal value)```

Encodes a decimal.

- Parameters:
	- ```value``` - The decimal to encode.

```void Encode (char value)```

Encodes a single char.

- Parameters:
	- ```value``` - The char to encode.

```void Encode (char [] values)```

Encodes a traditional signed int.

- Parameters:
	- ```values``` - An array of chars to encode.

```void Encode (string value)```

Encodes a string.

- Parameters:
	- ```value``` - The string to encode.

```void Encode (IEncodable encodable)```

Encodes an IEncodable object. Check [IEncodable](IEncodable.md) for more info.

- Parameters:
	- ```encodable``` - The IEncodable to encode.

### ```MessageEncoder.Decode ()```

Use any of these methods to decode data types.

```void Decode (out byte value)```

Decodes a single byte.

- Parameters:
	- ```out value``` - The decoded byte.

```void Decode (out byte [] values, int count = 0)```

Decodes multiple bytes.

- Parameters:
	- ```out values``` - The decoded array of bytes.

```void Decode (out sbyte value)```

Decodes a signed byte.

- Parameters:
	- ```out value``` - The decoded sbyte.

```void Decode (out bool value)```

Decodes a boolean.

- Parameters:
	- ```out value``` - The decoded bool.

```void Decode (out short value)```

Decodes a signed short integer.

- Parameters:
	- ```out value``` - The decoded short.

```void Decode (out ushort value)```

Decodes an unsigned short integer.

- Parameters:
	- ```out value``` - The decoded ushort.

```void Decode (out int value)```

Decodes an traditional signed integer.

- Parameters:
	- ```out value``` - The decoded int.

```void Decode (out uint value)```

Decodes an unsigned integer.

- Parameters:
	- ```out value``` - The decoded uint.

```void Decode (out long value)```

Decodes a long integer.

- Parameters:
	- ```out value``` - The decoded long.

```void Decode (out ulong value)```

Decodes an unsigned long integer.

- Parameters:
	- ```out value``` - The decoded ulong.

```void Decode (out float value)```

Decodes a float.

- Parameters:
	- ```out value``` - The decoded float.

```void Decode (out double value)```

Decodes a double.

- Parameters:
	- ```out value``` - The decoded double.

```void Decode (out decimal value)```

Decodes a decimal.

- Parameters:
	- ```out value``` - The decoded decimal.

```void Decode (out char value)```

Decodes a single char.

- Parameters:
	- ```out value``` - The decoded char.

```void Decode (out char [] values, int count = 0)```

Decodes multiple chars.

- Parameters:
	- ```out value``` - The decoded array of chars.
	- ```count``` - How many chars to decode.

```void Decode (out string value)```

Decodes a string.

- Parameters:
	- ```out value``` - The decoded string.

```void Decode (IEncodable encodable)```

Decodes an IEncodable object. Check [IEncodable](IEncodable.md) for more info.

- Parameters:
	- ```encodable``` - The object to decode.
