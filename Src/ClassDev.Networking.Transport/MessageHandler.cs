using System.Collections.Generic;

namespace ClassDev.Networking.Transport
{
	public class MessageHandler
	{
		/// <summary>
		/// The local id of the handler. This id is local to the subgroup.
		/// </summary>
		public byte id = 0;
		/// <summary>
		/// How deep the handler is (to how many subgroups does it belong).
		/// </summary>
		public byte depth = 0;

		/// <summary>
		/// The reference path to this message handler by ids.
		/// </summary>
		public byte [] signature = null;

		/// <summary>
		/// The handler callback definition.
		/// </summary>
		/// <param name="message"></param>
		public delegate void Callback (Message message);

		/// <summary>
		/// Optimized handlers.
		/// </summary>
		private MessageHandler [] optimizedHandlers;
		/// <summary>
		/// Unoptimized handlers.
		/// </summary>
		private List<MessageHandler> handlers;

		/// <summary>
		/// The handler (group) which this handler belongs to.
		/// </summary>
		public MessageHandler parentHandler { get; private set; }

		/// <summary>
		/// The callback of this handler.
		/// </summary>
		public Callback callback;

		/// <summary>
		/// Constructor.
		/// </summary>
		public MessageHandler ()
		{
			handlers = new List<MessageHandler> ();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callback"></param>
		private MessageHandler (Callback callback)
		{
			this.callback = callback;
		}

		/// <summary>
		/// Creates a sub-handler.
		/// </summary>
		/// <returns></returns>
		public MessageHandler SubHandler ()
		{
			MessageHandler messageHandler = new MessageHandler ();
			messageHandler.parentHandler = this;
			return messageHandler;
		}

		/// <summary>
		/// Registers a handler.
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public MessageHandler Register (Callback callback)
		{
			if (this.callback != null)
				throw new System.InvalidOperationException ("Trying to register a handler in a handler that is not a group. You can only register handlers in group handlers.");

			MessageHandler messageHandler = new MessageHandler (callback);

			handlers.Add (messageHandler);

			return messageHandler;
		}

		/// <summary>
		/// Handles the message.
		/// </summary>
		/// <param name="message"></param>
		public void Handle (Message message)
		{
			if (message == null)
				return;

			if (optimizedHandlers == null)
			{
				if (callback == null)
					// TODO: Throw an exception.
					return;

				callback (message);

				return;
			}

			message.encoder.Decode (out byte id);

			if (id >= optimizedHandlers.Length)
				// TODO: Throw an exception.
				return;

			if (optimizedHandlers [id] == null)
				// TODO: Throw an exception.
				return;

			optimizedHandlers [id].Handle (message);
		}

		/// <summary>
		/// Optimizes the handler and makes it ready for handling messages. You must call this on the root message handler.
		/// </summary>
		public void Optimize ()
		{
			if (parentHandler != null)
				throw new System.InvalidOperationException ("Optimize should be called on the root handler. There are necessary dependencies that need to be calculated from the root handler.");

			OptimizePrivate ();
		}

		/// <summary>
		/// Recursive optimizer.
		/// </summary>
		/// <param name="depth"></param>
		private void OptimizePrivate (int depth = 0)
		{
			this.depth = (byte)depth;

			signature = new byte [depth];
			MessageHandler parentHandler = this.parentHandler;
			for (int i = depth - 1; i >= 0; i--)
			{
				signature [i] = id;

				if (parentHandler == null)
					break;

				parentHandler = parentHandler.parentHandler;
			}

			if (handlers == null)
				return;

			optimizedHandlers = new MessageHandler [handlers.Count];

			for (int i = 0; i < handlers.Count; i++)
			{
				if (handlers [i] == null)
					continue;

				optimizedHandlers [i] = handlers [i];
				optimizedHandlers [i].id = (byte)i;

				optimizedHandlers [i].OptimizePrivate (depth + 1);
			}
		}
	}
}
