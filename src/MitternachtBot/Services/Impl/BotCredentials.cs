using System;
using System.Collections.Immutable;
using System.IO;
using Discord;
using Mitternacht.Common;
using Newtonsoft.Json;
using NLog;

namespace Mitternacht.Services.Impl {
	public class BotCredentials : IBotCredentials {
		public ulong  ClientId                { get; set; } = 0;
		public string Token                   { get; set; } = "";
		public string DbConnectionString      { get; set; } = "Filename=./data/MitternachtBot.db";
		public string DbConnection            { get; set; } = "Host=127.0.0.1;Port=5432;Database=mitternachtbot;Username=mitternachtbot;Password=mitternachtbotpassword;";

		public ImmutableArray<ulong> OwnerIds { get; set; } = new ulong[1].ToImmutableArray();
		
		public string GoogleApiKey            { get; set; } = "";
		public string MashapeKey              { get; set; } = "";
		public string LoLApiKey               { get; set; } = "";
		public string OsuApiKey               { get; set; } = "";
		public string CleverbotApiKey         { get; set; } = "";
		public string CarbonKey               { get; set; } = "";
		public string PatreonAccessToken      { get; set; } = "";
		public string PatreonCampaignId       { get; set; } = "";

		public int    TotalShards             { get; set; } = 1;
		public string ShardRunCommand         { get; set; } = "";
		public string ShardRunArguments       { get; set; } = "";
		public int?   ShardRunPort            { get; set; } = null;

		public string ForumUsername           { get; set; } = "";
		public string ForumPassword           { get; set; } = "";

		private static readonly string _credsFileName = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");

		private BotCredentials() { }

		public static void WriteCredentialsExampleFile() {
			File.WriteAllText("./credentials_example.json", JsonConvert.SerializeObject(new BotCredentials(), Formatting.Indented));
		}

		public static BotCredentials Load() {
			var log = LogManager.GetCurrentClassLogger();

			try { WriteCredentialsExampleFile(); } catch { }

			if(!File.Exists(_credsFileName)) {
				log.Error($"'credentials.json' is missing. Add it and restart the program. An example can be found in {Path.GetFullPath("./credentials_example.json")}");
				Environment.Exit(3);
			}

			try {
				var credsFileContent = File.ReadAllText(_credsFileName);
				var creds            = JsonConvert.DeserializeObject<BotCredentials>(credsFileContent);
				
				if(string.IsNullOrWhiteSpace(creds.Token)) {
					log.Error($"Token is missing from '{_credsFileName}'. Add it and restart the program.");
					Environment.Exit(3);
				}

				creds.ShardRunPort     ??= new NadekoRandom().Next(5000, 6000);
				creds.TotalShards        = Math.Max(1, creds.TotalShards);
				creds.DbConnectionString = string.IsNullOrWhiteSpace(creds.DbConnectionString) ? "Filename=./data/MitternachtBot.db" : creds.DbConnectionString;

				return creds;
			} catch(Exception ex) {
				log.Fatal(ex.Message);
				log.Fatal(ex);
				throw;
			}
		}

		public bool IsOwner(IUser u) => OwnerIds.Contains(u.Id);
	}
}
