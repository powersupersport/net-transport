using NUnit.Framework;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ClassDev.Networking.Transport.LowLevel;

#pragma warning disable IDE1006 // Naming Styles

namespace Tests
{
    public class MessageManagerTest
    {
		UdpClient udpClient1 = null;
		UdpClient udpClient2 = null;

		IPEndPoint endPoint1 = null;
		IPEndPoint endPoint2 = null;

		MessageManager messageManager1 = null;
		MessageManager messageManager2 = null;

		[SetUp]
		public void Setup ()
		{
			udpClient1 = new UdpClient (7777);
			udpClient2 = new UdpClient (7778);

			IPAddress ipAddress = IPAddress.Parse ("127.0.0.1");

			endPoint1 = new IPEndPoint (ipAddress, 7777);
			endPoint2 = new IPEndPoint (ipAddress, 7778);
		}

		[TearDown]
		public void Shutdown ()
		{
			if (messageManager1 != null)
				messageManager1.Stop ();

			if (messageManager2 != null)
				messageManager2.Stop ();

			udpClient1.Close ();
			udpClient2.Close ();

			udpClient1.Dispose ();
			udpClient2.Dispose ();
		}

		// TESTS BEGIN ------------------------------------------------------------------

        [Test]
		public void message_manager_can_be_started_and_stopped ()
		{
			messageManager1 = new MessageManager (udpClient1);

			Assert.That (messageManager1.isStarted);

			messageManager1.Stop ();

			Assert.That (!messageManager1.isStarted);
        }

		[Test]
		public void message_can_be_sent_and_received ()
		{
			messageManager1 = new MessageManager (udpClient1);
			messageManager2 = new MessageManager (udpClient2);

			Message message = new Message (endPoint2, 3);
			message.encoder.Encode (new byte [] { 1, 2, 3 });

			messageManager1.Send (message);

			for (int i = 0; i < 20; i++)
			{
				message = messageManager2.Receive ();

				if (message == null)
				{
					Thread.Sleep (10);
					continue;
				}

				Assert.That (message.buffer [0] == 1);
				Assert.That (message.buffer [1] == 2);
				Assert.That (message.buffer [2] == 3);

				break;
			}

			Assert.That (message != null, "Message was not received.");
		}
    }
}

#pragma warning restore IDE1006 // Naming Styles
