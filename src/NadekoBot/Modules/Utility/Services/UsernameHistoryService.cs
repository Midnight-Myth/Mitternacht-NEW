using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services;

namespace Mitternacht.Modules.Utility.Services
{
    public class UsernameHistoryService : INService
    {
        private readonly DbService _db;

        public UsernameHistoryService(DiscordSocketClient client, DbService db) {
            _db = db;
            client.UserJoined += UserJoined;
            client.UserUpdated += UserUpdated;
        }

        private async Task UserJoined(SocketGuildUser user) {
            if (!IsGuildLoggingUsernames(user.Guild.Id)) return;
            var nick = string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname;
            using (var uow = _db.UnitOfWork)
            {
                if (string.IsNullOrWhiteSpace(user.Nickname))
                    uow.UsernameHistory.AddUsername(user.Id, nick);
                else uow.NicknameHistory.AddUsername(user.Guild.Id, user.Id, nick);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task UserUpdated(SocketUser before, SocketUser after) {
            if (!(before is IGuildUser b) || !(after is IGuildUser a) || IsGuildLoggingUsernames(a.GuildId)) return;
            var nickbefore = string.IsNullOrWhiteSpace(b.Nickname) ? b.Username : b.Nickname;
            var nickafter = string.IsNullOrWhiteSpace(a.Nickname) ? a.Username : a.Nickname;
            if(string.Equals(nickbefore, nickafter, StringComparison.Ordinal)) return;

            using (var uow = _db.UnitOfWork)
            {
                if (string.IsNullOrWhiteSpace(b.Nickname))
                    uow.UsernameHistory.AddUsername(a.Id, nickafter);
                else uow.NicknameHistory.AddUsername(a.GuildId, a.Id, nickafter);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public bool IsGuildLoggingUsernames(ulong guildId) {
            bool globalLogging;
            bool? guildLogging;
            using (var uow = _db.UnitOfWork) {
                globalLogging = uow.BotConfig.GetOrCreate(set => set.Include(s => s.LogUsernames)).LogUsernames;
                guildLogging = uow.GuildConfigs.For(guildId, set => set.Include(s => s.LogUsernameHistory)).LogUsernameHistory;
            }

            return guildLogging ?? globalLogging;
        }
    }
}
