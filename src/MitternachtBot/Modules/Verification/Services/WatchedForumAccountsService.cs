using Discord;
using GommeHDnetForumAPI;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Impl;
using System.Linq;
using System.Threading.Tasks;

namespace Mitternacht.Modules.Verification.Services {
	public class WatchedForumAccountsService : IMService {
		private readonly DbService _db;
		private readonly VerificationService _vs;
		private readonly StringService _ss;

		public WatchedForumAccountsService(DbService db, VerificationService vs, StringService ss) {
			_db = db;
			_vs = vs;
			_ss = ss;

			_vs.UserVerified += UserVerified;
		}

		private async Task UserVerified(IGuildUser guildUser, long forumUserId) {
			using var uow = _db.UnitOfWork;
			var wfa = uow.WatchedForumAccounts.GetForGuild(guildUser.GuildId).FirstOrDefault(wfa => wfa.ForumUserId == forumUserId);

			if(wfa is not null) {
				switch(wfa.WatchAction) {
					case Database.Models.WatchAction.NONE:
						break;
					case Database.Models.WatchAction.NOTIFY:
						var channelId = uow.GuildConfigs.For(guildUser.GuildId).ForumAccountWatchNotificationChannelId;

						if(channelId.HasValue) {
							var channel = await guildUser.Guild.GetTextChannelAsync(channelId.Value).ConfigureAwait(false);

							if(channel is not null) {
								await channel.SendConfirmAsync(GetText("userverified_notify_description", guildUser.GuildId, guildUser.Mention, guildUser.ToString(), $"{forumUserId}", $"{ForumPaths.MembersUrl}{forumUserId}"), GetText("userverified_notify_title", guildUser.GuildId)).ConfigureAwait(false);
							}
						}
						break;
					case Database.Models.WatchAction.BAN:
						await guildUser.BanAsync(reason: GetText("userverified_ban_reason", guildUser.GuildId)).ConfigureAwait(false);
						break;
					default:
						break;
				}
			}
		}

		private string GetText(string key, ulong? guildId, params string[] replacements)
			=> _ss.GetText("verification", key, guildId, replacements);
	}
}
