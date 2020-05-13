using System.Collections.Immutable;
using Discord;

namespace Mitternacht.Services {
	public interface IBotCredentials {
		ulong                 ClientId           { get; }
		string                Token              { get; }
		ImmutableArray<ulong> OwnerIds           { get; }
		string                DbConnectionString { get; }
		
		string                GoogleApiKey       { get; }
		string                MashapeKey         { get; }
		string                LoLApiKey          { get; }
		string                OsuApiKey          { get; }
		string                CleverbotApiKey    { get; }
		string                CarbonKey          { get; }
		string                PatreonCampaignId  { get; }
		string                PatreonAccessToken { get; }
		
		int                   TotalShards        { get; }
		string                ShardRunCommand    { get; }
		string                ShardRunArguments  { get; }
		int?                  ShardRunPort       { get; }
		
		string                ForumUsername      { get; }
		string                ForumPassword      { get; }

		bool IsOwner(IUser u);
	}
}
