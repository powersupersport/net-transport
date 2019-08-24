using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ClassDev.Networking.Transport;

namespace Tests
{
    public class HostTest
    {
		[Test]
		public void A_host_can_send_a_message_to_another_host ()
		{
			Host host1 = new Host (7777);
			Host host2 = new Host (7778);

			host1.Start ();
			host2.Start ();

			Message message = new Message (host2.endPoint, host1.messageHandler.genericHandler, 6);

			host1.Send (message);

			for (int i = 0; i < 6; i++)
			{
				message = host2.Receive ();
				if (message != null)
				{
					Assert.True (true);
					return;
				}

				System.Threading.Thread.Sleep (10);
			}

			Assert.False (true, "No messages were received.");
		}
    }
}
