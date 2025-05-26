namespace ArchipelagoDiscord
{
	static class Program
	{
		public static void Main(String[] args)
		{
			ArchipelagoDiscord APD = new ArchipelagoDiscord();
			APD.StartArchipelagoClient();
			while( true )
				Task.Delay(1000).Wait();
		}
	}
}
