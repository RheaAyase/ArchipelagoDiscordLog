using Archipelago.MultiClient.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoDiscord
{
	class ArchipelagoDiscord
	{
		private readonly Config Config;

		private readonly string ApVersion = "0.6.2";
		private readonly string[] ApTags = new string[]{ "TextOnly" };

		private readonly HttpClient HttpClient = new HttpClient();
		private readonly DateTime TimeStarted = DateTime.UtcNow;
		private readonly TimeSpan TimeDelay = TimeSpan.FromSeconds(30);
		private readonly Regex IgnoredMessagesRegex;

		private readonly Dictionary<string, UserPreference> UserPreferences;

		public ArchipelagoDiscord()
		{
			this.Config = Config.Load() ?? throw new NullReferenceException();
			this.IgnoredMessagesRegex = new Regex(this.Config.IgnoredMessagesRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
			this.UserPreferences = this.Config.UserPreferences.ToDictionary(u => u.ApSlotName, u => u);
		}

		public void StartArchipelagoClient()
		{
			ArchipelagoSession session = ArchipelagoSessionFactory.CreateSession(this.Config.ApHost, this.Config.ApPort);
			//session.Items.ItemReceived += OnItemReceived;
			session.MessageLog.OnMessageReceived += OnMessageReceived;
			LoginResult loginResult = session.TryConnectAndLogin(null, this.Config.ApSlot, ItemsHandlingFlags.AllItems, Version.Parse(this.ApVersion), this.ApTags, requestSlotData: false);
			if( loginResult.Successful )
				Console.WriteLine("I haz connected.");
			else
				Console.WriteLine($"Failed to connect to the AP server:\n- `{((loginResult as LoginFailure)?.Errors[0] ?? "Unknown Error")}`");

			Task.Delay(this.TimeDelay).Wait();
			SendWebhook("I haz connected.").Wait();
		}

		private void OnMessageReceived(LogMessage msg)
		{
			if( (this.TimeStarted + this.TimeDelay) > DateTime.UtcNow )
				return;

			if( this.IgnoredMessagesRegex.IsMatch(msg.ToString()) )
			{
				//Console.WriteLine($"Skipping message: {msg.ToString()}");
				return;
			}

			if( msg.Parts.Length <= 1 || (!msg.Parts[1].Text.Contains("sent") && !msg.Parts[1].Text.Contains("found") && !msg.Parts[0].Text.Contains("Hint")) )
			{
				//Console.WriteLine($"Plain text to webhook: {msg.ToString()}");
				SendWebhook(msg.ToString()).Wait();
				return;
			}

			UserPreference? receiverPref = null;
			string sender = msg.Parts[0].Text;
			if( msg.Parts[0].Text.Contains("Hint") )
				sender = msg.Parts[1].Text;

			StringBuilder stringBuilder = new StringBuilder();
			foreach( MessagePart part in msg.Parts )
			{
				//Console.WriteLine($"{part.Text}");
				switch( part.Type )
				{
					case MessagePartType.Player:
						if( sender != part.Text && this.UserPreferences.ContainsKey(part.Text) )
							receiverPref = this.UserPreferences[part.Text];

						stringBuilder.Append($"`{part.Text}`");
						break;
					case MessagePartType.Item:
						stringBuilder.Append($"**{part.Text}**");
						break;
					case MessagePartType.Location:
						stringBuilder.Append($"*{part.Text}*");
						break;
					default:
						if( receiverPref == null && part.Text == "found their" && this.UserPreferences.ContainsKey(sender) )
							receiverPref = this.UserPreferences[sender];

						stringBuilder.Append(part.Text);
						break;
				}
			}

			if( receiverPref != null && receiverPref.DiscordMention && receiverPref.DiscordId != 0 )
				stringBuilder.Append($" <@{receiverPref.DiscordId}> ({receiverPref.Game})");
			SendWebhook(stringBuilder.ToString()).Wait();
		}

		private async Task SendWebhook(string text)
		{
			StringContent content = new StringContent(JsonConvert.SerializeObject(new{ content = text }), Encoding.UTF8, "application/json");
			await HttpClient.PostAsync(this.Config.DiscordWebhookUrl, content);
		}

		/*private void OnItemReceived(ItemInfo item, ReceivedItemsHelper helper)
		{
			Console.WriteLine($"Received: {item.ItemName}");
			if( (this.TimeStarted + this.TimeDelay) > DateTime.UtcNow )
				return;
			string userKey = item.ItemGame;
			NotifyItemReceived(userKey, item.ItemGame, item.ItemName, item.Player.Name).Wait();
		}

		private async Task NotifyItemReceived(string userKey, string game, string item, string fromuser)
		{
			UInt64 userid = 0;
			if( this.UserIDs.ContainsKey(userKey) )
				userid = this.UserIDs[userKey];

			string message = (userid == 0 ? "Everyone" : $"<@{userid}>") + $" ({game}) received {item} from {fromuser}";
			await SendWebhook(message);
		}*/
	}
}
