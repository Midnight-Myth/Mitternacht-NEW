using System.Collections.Immutable;
using Discord;
using Mitternacht.Common;

namespace Mitternacht.Services
{
    public interface IBotCredentials
    {
        ulong ClientId { get; }

        string Token { get; }
        string GoogleApiKey { get; }
        ImmutableArray<ulong> OwnerIds { get; }
        string MashapeKey { get; }
        string LoLApiKey { get; }
        string PatreonAccessToken { get; }
        string CarbonKey { get; }

        DbConfig Db { get; }
        string OsuApiKey { get; }

        bool IsOwner(IUser u);
        int TotalShards { get; }
        string ShardRunCommand { get; }
        string ShardRunArguments { get; }
        string PatreonCampaignId { get; }
        string CleverbotApiKey { get; }
        string ForumUsername { get; }
        string ForumPassword { get; }
    }
}
