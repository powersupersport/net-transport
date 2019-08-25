namespace ClassDev.Networking.Transport
{
	public struct MessageChannelTemplate
	{
		public bool isReliable;
		public bool isSequenced;

		public MessageChannelTemplate (bool isReliable = false, bool isSequenced = false)
		{
			this.isReliable = isReliable;
			this.isSequenced = isSequenced;
		}
	}
}
