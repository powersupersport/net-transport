using ClassDev.Networking.Transport;
using NUnit.Framework;
using System.Net;
using System.Threading;

#pragma warning disable IDE1006 // Naming Styles
namespace Tests
{
    public class HostTest
	{
		Host host = null;
		Host host1 = null;
		Host host2 = null;

		Message message = null;

		[SetUp]
		public void SetUp ()
		{

		}

		[TearDown]
		public void TearDown ()
		{
			if (host != null)
				host.Stop ();

			if (host1 != null)
				host1.Stop ();

			if (host2 != null)
				host2.Stop ();

			message = null;
		}

        [Test]
		public void host_can_be_created_and_stopped ()
		{
			host = new Host (7777, 1);

			Assert.That (host.port == 7777);

			host.Start ();

			Assert.That (host.isStarted);

			Assert.NotNull (host.messageManager);
			Assert.NotNull (host.messageHandler);
			Assert.NotNull (host.udpClient);
			Assert.NotNull (host.stopwatch);

			host.Stop ();

			Assert.That (!host.isStarted);

			Assert.Null (host.messageManager);
			Assert.Null (host.messageHandler);
			Assert.Null (host.udpClient);
			Assert.Null (host.stopwatch);
		}

		[Test]
		public void host_can_connect_to_another_host ()
		{
			host1 = new Host (7777, 1);
			host2 = new Host (7778, 1);
			host1.Start ();
			host2.Start ();

			Connection connection = host1.Connect ("127.0.0.1", 7778);

			Assert.NotNull (connection, "Connect doesn't return a connection.");

			for (int i = 0; i < 30; i++)
			{
				host1.Update ();
				host2.Update ();

				if (connection.isSuccessful)
					break;

				Thread.Sleep (10);
			}

			Assert.That (connection.isSuccessful);

			Assert.That (host2.ResolveConnection ("127.0.0.1", 7777) != null);
		}

		[Test]
		public void both_hosts_can_send_messages_to_each_other ()
		{
			host1 = new Host (7777, 1);
			host2 = new Host (7778, 1);

			host1.Start ();
			host2.Start ();

			MessageHandler messageHandler1 = host1.messageHandler.Register (HandleMessage);
			host1.messageHandler.Optimize ();

			MessageHandler messageHandler2 = host2.messageHandler.Register (HandleMessage);
			host2.messageHandler.Optimize ();

			Connection connection = host1.Connect ("127.0.0.1", 7778);

			for (int i = 0; i < 20; i++)
			{
				host1.Update ();
				host2.Update ();

				if (connection.isSuccessful)
					break;

				Thread.Sleep (10);
			}

			message = new Message (connection, messageHandler1, 0, 1);
			message.encoder.Encode ((byte)1);
			host1.Send (message);
			message = null;

			for (int i = 0; i < 20; i++)
			{
				host1.Update ();
				host2.Update ();

				if (message != null)
					break;

				Thread.Sleep (10);
			}

			Assert.That (message != null, "No message was received!");
			message.encoder.Decode (out byte value1);
			Assert.That (value1 == 1);

			connection = host2.ResolveConnection ("127.0.0.1", 7777);

			message = new Message (connection, messageHandler2, 0, 1);
			message.encoder.Encode ((byte)1);
			host2.Send (message);
			message = null;

			for (int i = 0; i < 20; i++)
			{
				host1.Update ();
				host2.Update ();

				if (message != null)
					break;

				Thread.Sleep (10);
			}

			Assert.That (message != null, "No message was received!");
			message.encoder.Decode (out byte value2);
			Assert.That (value2 == 1);
		}

		private void HandleMessage (Message message)
		{
			this.message = message;
		}
    }
}
#pragma warning restore IDE1006 // Naming Styles
