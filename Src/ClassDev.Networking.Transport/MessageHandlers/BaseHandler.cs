namespace ClassDev.Networking.Transport
{
	public class BaseHandler : MessageHandler
	{
		public MessageHandler connectionHandler;
		public MessageHandler disconnectionHandler;
		public MessageHandler acknowledgementHandler;
		public MessageHandler multipleHandler;
		public MessageHandler fragmentHandler;
		public MessageHandler genericHandler;

		public BaseHandler () : base ()
		{

		}
	}
}
