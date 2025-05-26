using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace ArchipelagoDiscord
{
	[Serializable]
	public class UserPreference
	{
		public string ApSlotName;
		public UInt64 DiscordId;
		public bool DiscordMention;
		public string? Game;

		public UserPreference(string apSlotName, UInt64 discordId, bool discordMention, string game)
		{
			this.ApSlotName = apSlotName;
			this.DiscordId = discordId;
			this.DiscordMention = discordMention;
			this.Game = game;
		}
	}

	public class Config
	{
		public const string Filename = "config.json";
		private Object _Lock = new Object();

		public string DiscordWebhookUrl = "https://discord.com/api/webhooks/12341234/asdfasdf";
		public string ApHost = "127.0.0.1";
		public int ApPort = 38281;
		public string ApSlot = "ArchipelagoDiscord";
		public string IgnoredMessagesRegex = "(tracking|viewing|changed\\stags|!hint)";
		public UserPreference[] UserPreferences = new[]{ new UserPreference("Name of my AP Slot", 89805412676681728, true, "Game I Play") };

		public static Config? Load()
		{
			if( !File.Exists(Filename) )
			{
				string json = JsonConvert.SerializeObject(new Config(), Formatting.Indented);
				File.WriteAllText(Filename, json);
			}

			Config? newConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Filename));
			return newConfig;
		}

		public void SaveAsync()
		{
			Task.Run(() => Save());
		}

		private void Save()
		{
			lock( this._Lock )
			{
				string json = JsonConvert.SerializeObject(this, Formatting.Indented);
				File.WriteAllText(Filename, json);
			}
		}
	}
}
