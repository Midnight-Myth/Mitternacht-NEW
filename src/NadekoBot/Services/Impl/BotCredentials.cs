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
		public ulong    ClientId              { get; }
		public string   Token                 { get; }
		public DbConfig Db                    { get; } = new DbConfig("sqlite", "Filename=./data/MitternachtBot.db");

		public ImmutableArray<ulong> OwnerIds { get; }
		
		public string GoogleApiKey            { get; }
		public string MashapeKey              { get; }
		public string LoLApiKey               { get; }
		public string OsuApiKey               { get; }
		public string CleverbotApiKey         { get; }
		public string CarbonKey               { get; }
		public string PatreonAccessToken      { get; }
		public string PatreonCampaignId       { get; }

		public int    TotalShards             { get; } = 1;
		public string ShardRunCommand         { get; }
		public string ShardRunArguments       { get; }
		public int    ShardRunPort            { get; }

		public string ForumUsername           { get; }
		public string ForumPassword           { get; }

		private readonly string _credsFileName = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");

		public BotCredentials() {
			var log = LogManager.GetCurrentClassLogger();

			try {
				File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new CredentialsModel(), Formatting.Indented));
			} catch { }

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
				var dbSection = data.GetSection("db");
				Db            = new DbConfig(string.IsNullOrWhiteSpace(dbSection["Type"]) ? "sqlite" : dbSection["Type"], string.IsNullOrWhiteSpace(dbSection["ConnectionString"]) ? "Filename=./data/MitternachtBot.db" : dbSection["ConnectionString"]);

				ForumUsername = data[nameof(ForumUsername)];
				ForumPassword = data[nameof(ForumPassword)];
			} catch(Exception ex) {
				log.Fatal(ex.Message);
				log.Fatal(ex);
				throw;
			}

		}

		private class CredentialsModel {
			public ulong   ClientId           { get; set; } = 0;
			public string  Token              { get; set; } = "";
			public ulong[] OwnerIds           { get; set; } = new ulong[1];

			public string  GoogleApiKey       { get; set; } = "";
			public string  MashapeKey         { get; set; } = "";
			public string  LoLApiKey          { get; set; } = "";
			public string  OsuApiKey          { get; set; } = "";
			public string  CleverbotApiKey    { get; set; } = "";
			public string  CarbonKey          { get; set; } = "";
			public string  PatreonAccessToken { get; set; } = "";
			public string  PatreonCampaignId  { get; set; } = "";

			public DbConfig Db                { get; set; } = new DbConfig("sqlite", "Filename=./data/MitternachtBot.db");

			public int     TotalShards        { get; set; } = 1;
			public string  ShardRunCommand    { get; set; } = "";
			public string  ShardRunArguments  { get; set; } = "";
			public int?    ShardRunPort       { get; set; } = null;

			public string  ForumUsername      { get; set; } = null;
			public string  ForumPassword      { get; set; } = null;
		}

		public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
	}
}
