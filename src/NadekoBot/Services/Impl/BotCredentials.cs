using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Discord;
using Microsoft.Extensions.Configuration;
using Mitternacht.Common;
using Newtonsoft.Json;
using NLog;

namespace Mitternacht.Services.Impl {
	public class BotCredentials : IBotCredentials {
		public ulong  ClientId                { get; private set; } = 0;
		public string Token                   { get; private set; } = "";
		public string DbConnectionString      { get; private set; } = "Filename=./data/MitternachtBot.db";

		public ImmutableArray<ulong> OwnerIds { get; private set; } = new ulong[1].ToImmutableArray();
		
		public string GoogleApiKey            { get; private set; } = "";
		public string MashapeKey              { get; private set; } = "";
		public string LoLApiKey               { get; private set; } = "";
		public string OsuApiKey               { get; private set; } = "";
		public string CleverbotApiKey         { get; private set; } = "";
		public string CarbonKey               { get; private set; } = "";
		public string PatreonAccessToken      { get; private set; } = "";
		public string PatreonCampaignId       { get; private set; } = "";

		public int    TotalShards             { get; private set; } = 1;
		public string ShardRunCommand         { get; private set; } = "";
		public string ShardRunArguments       { get; private set; } = "";
		public int?   ShardRunPort            { get; private set; } = null;

		public string ForumUsername           { get; private set; } = "";
		public string ForumPassword           { get; private set; } = "";

		private readonly string _credsFileName = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");

		public BotCredentials() {
			Load();
		}

		private BotCredentials(bool loadCredentials) {
			if(loadCredentials) {
				Load();
			}
		}

		public void WriteCredentialsExampleFile() {
			File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new BotCredentials(false), Formatting.Indented));
		}

		public void Load() {
			var log = LogManager.GetCurrentClassLogger();

			try { WriteCredentialsExampleFile(); } catch { }

			if(!File.Exists(_credsFileName)) {
				log.Warn($"credentials.json is missing. Attempting to load creds from environment variables prefixed with 'NadekoBot_'. Example is in {Path.GetFullPath("./credentials_example.json")}");
			}

			try {
				var configBuilder = new ConfigurationBuilder();
				configBuilder.AddJsonFile(_credsFileName, true).AddEnvironmentVariables("NadekoBot_");

				var data = configBuilder.Build();

				Token = data[nameof(Token)];
				if(string.IsNullOrWhiteSpace(Token)) {
					log.Error($"Token is missing from '{_credsFileName}' or Environment variables. Add it and restart the program.");
					Environment.Exit(3);
				}
				OwnerIds           = data.GetSection("OwnerIds").GetChildren().Select(c => ulong.Parse(c.Value)).ToImmutableArray();
				LoLApiKey          = data[nameof(LoLApiKey)];
				GoogleApiKey       = data[nameof(GoogleApiKey)];
				MashapeKey         = data[nameof(MashapeKey)];
				OsuApiKey          = data[nameof(OsuApiKey)];
				PatreonAccessToken = data[nameof(PatreonAccessToken)];
				PatreonCampaignId  = data[nameof(PatreonCampaignId)];
				ShardRunCommand    = data[nameof(ShardRunCommand)];
				ShardRunArguments  = data[nameof(ShardRunArguments)];
				CleverbotApiKey    = data[nameof(CleverbotApiKey)];
				if(string.IsNullOrWhiteSpace(ShardRunCommand))
					ShardRunCommand = "dotnet";
				if(string.IsNullOrWhiteSpace(ShardRunArguments))
					ShardRunArguments = "run -c Release -- {0} {1} {2}";

				var portStr = data[nameof(ShardRunPort)];
				ShardRunPort = string.IsNullOrWhiteSpace(portStr) ? new NadekoRandom().Next(5000, 6000) : int.Parse(portStr);

				int.TryParse(data[nameof(TotalShards)], out var ts);
				TotalShards = ts < 1 ? 1 : ts;

				ulong.TryParse(data[nameof(ClientId)], out var clId);
				ClientId = clId;

				CarbonKey     = data[nameof(CarbonKey)];
				DbConnectionString = string.IsNullOrWhiteSpace(data[nameof(DbConnectionString)]) ? "Filename=./data/MitternachtBot.db" : data[nameof(DbConnectionString)];

				ForumUsername = data[nameof(ForumUsername)];
				ForumPassword = data[nameof(ForumPassword)];
			} catch(Exception ex) {
				log.Fatal(ex.Message);
				log.Fatal(ex);
				throw;
			}
		}

		public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
	}
}
