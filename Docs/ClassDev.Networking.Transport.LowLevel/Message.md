# Message

Message is an object that contains the data of a message, plus some info about the message itself. Message is used both for sending and receiving, so it has some *polymorphic* logic in place.

*polymorphic - logic that serves different purposes in different scenarios*

*Explain the construction of messages and how they work...*

&nbsp;

## Properties

### ```Message.BufferSize```
```const int BufferSize = 1024```

The default size of the message buffer.

### ```Message.buffer```
```byte [] buffer { get; set; }```

The raw buffer containing the raw bytes. Do not modify this unless you know what you're doing. Consider using the ```encoder``` instead.

### ```Message.endPoint```
```IPEndPoint endPoint { get; set; }```

If the message is meant to be sent, then this is the target's IP address and port. If the message is received, then this is the sender's IP address and port.

### ```Message.encoder```
```MessageEncoder encoder { get; }```

The encoder used for encoding/decoding messages. This is the object that you're going to use to read/write data to your messages. It's really neat to use.

Take a look at [MessageEncoder](MessageEncoder.md) for more info.

&nbsp;

## Methods

### ```Message ()```
Constructor for Message.

**Since the message class has polymorphic behaviour, it is very important to use the correct constructor.**

 ```Message (IPEndPoint endPoint, int bufferSize = 1024)```

[SEND] Use this constructor if the message is to be sent.

- Parameters:
	- ```endPoint``` - The IP address and port of the target remote receiver.
	- ```bufferSize``` - The space allocated for the message. It's recommended that you keep that as low as possible to save memory. Perfect scenario would be if you know exactly how many bytes you're going to write to that message. **However, this parameter does not set how many bytes are going to be sent. The actual sent message is being trimmed to only include the written data. The buffer size only helps to save memory locally.**

```Message (IPEndPoint endPoint, byte [] data)```

[RECEIVE] Use this constructor if the message is received. In a normal scenario you wouldn't (and preferrably shouldn't) use this constructor as the message will already be created for you by the ```MessageManager.Receive ()```.

- Parameters:
	- ```endPoint``` - The IP address and port of the sender.
	- ```byte [] data``` - The data.

Use this constructor if the message is received.

### ```Message.Encode ()```
Encodes an object that implements the ```IEncodable``` interface. You can read more about it [here](IEncodable.md).

```void Encode (IEncodable encodable)```

- Parameters:
	- ```encodable``` - The object to encode.

### ```Message.Decode ()```
Decodes an object that implements the ```IEncodable``` interface. You can read more about it [here](IEncodable.md).

```void Decode (IEncodable encodable)```

- Parameters:
	- ```encodable``` - The object to decode.

### ```Message.ToString ()```
Returns an array representation of the message as a string.

```string ToString ()```

- Returns:
	- ```string``` - The array representation string.