using System.Collections.Generic;

namespace ClassDev.Networking.Transport
{
	public class MessageHandler
	{
		/// <summary>
		/// The local id of the handler. This id is local to the subgroup.
		/// </summary>
		public byte id { get; private set; }
		/// <summary>
		/// How deep the handler is (to how many subgroups does it belong).
		/// </summary>
		public byte depth { get; private set; }

		/// <summary>
		/// The reference path to this message handler by ids.
		/// </summary>
		public byte [] signature { get; private set; }

		/// <summary>
		/// The handler callback definition.
		/// </summary>
		/// <param name="message"></param>
		public delegate void Callback (Message message);

		/// <summary>
		/// Optimized handlers.
		/// </summary>
		private MessageHandler [] optimizedHandlers = null;
		/// <summary>
		/// Unoptimized handlers.
		/// </summary>
		private List<MessageHandler> handlers = null;

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
				throw new System.InvalidOperationException ("Trying to register a handler in a handler that is not a branch. You can only register handlers in branch handlers.");

			if (handlers.Count >= 256)
				throw new System.InvalidOperationException ("The message handler is full! You can only register up to 256 handlers in one branch. Create a branched handler and put the new handler inside.");

			MessageHandler messageHandler = new MessageHandler (callback);
			handlers.Add (messageHandler);

			return messageHandler;
		}

		/// <summary>
		/// Resolves the handler of the message.
		/// </summary>
		/// <param name="message"></param>
		public MessageHandler ResolveHandler (Message message)
		{
			if (message == null)
				return null;

			if (optimizedHandlers == null)
			{
				if (callback == null)
					return null;

				return this;
			}

			byte id = 0;

			try
			{
				message.encoder.Decode (out id);
			}
			catch (System.Exception)
			{
				return null;
			}

			if (id >= optimizedHandlers.Length)
				return null;

			if (optimizedHandlers [id] == null)
				return null;

			return optimizedHandlers [id].ResolveHandler (message);
		}

		/// <summary>
		/// Optimizes the handler and all children handlers by caching all handler callbacks. You must call this on the root message handler.
		/// </summary>
		public void Optimize ()
		{
			OptimizePrivate (depth, this.signature);
		}

		/// <summary>
		/// Recursive optimizer.
		/// </summary>
		/// <param name="depth"></param>
		/// <param name="signature"></param>
		private void OptimizePrivate (int depth = 0, byte [] signature = null)
		{
			if (signature == null)
				signature = new byte [1];

			this.depth = (byte)depth;

			this.signature = new byte [depth];

			if (depth > 0)
			{
				for (int i = 0; i < signature.Length; i++)
				{
					this.signature [i] = signature [i];
				}
				this.signature [depth - 1] = id;
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

				optimizedHandlers [i].OptimizePrivate (depth + 1, this.signature);
			}
		}
	}
}
