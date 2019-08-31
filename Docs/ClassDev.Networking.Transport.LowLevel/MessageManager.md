# Message Manager

MessageManager depends on ```System.Net.Sockets```.

The message manager is a class that provides methods for sending/receiving messages in a thread-safe manner. It uses the ```Message``` class to construct a message with the necessary info.

---

## Initialization

---

### ```MessageManager ()```
Constructor for the MessageManager object.

```MessageManager (UdpClient udpClient)```

- Parameters:
	- ```udpClient``` - The UdpClient to use for sending/receiving messages. Will throw an exception if null.

### ```MessageManager.isStarted```
```bool isStarted```
True if the message manager is working. False if stopped.


### ```MessageManager.Stop ()```
```void Stop ()```

Will stop the send & receive threads, as well as unallocate all queues.

**You should call this before you close your application, as well as set your MessageManager object to null to allow for the garbage collector to clear the memory.**

---

## Sending / Receiving

---

### ```MessageManager.Send ()```
Used to enqueue a message to the send queue.

```void Send (Message message)```

- Parameters:
	- ```message``` - The message to send. Will throw an exception if null.

```void Send (IPEndPoint endPoint, byte [] message)```

- Parameters:
	- ```endPoint``` - A System.Net.IPEndPoint object containing the IP address and the port for the target remote location. Check the [MSDN docs](https://docs.microsoft.com/en-us/dotnet/api/system.net.ipendpoint) for more info.
	- ```message``` - A byte array with the message.


### ```MessageManager.Receive ()```
Used to receive a message from the queue.

```Message Receive ()```


- Returns:
	- ```Message``` - A message object with the received message inside. Visit the ```Message``` class for more info.
	- ```null``` - If there are no messages in the queue 

Returns a message object with the message inside, or null if there are no messages in the receive queue.
