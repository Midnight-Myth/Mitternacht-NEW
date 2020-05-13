using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Services;
using NLog;

namespace Mitternacht.Modules.Utility.Services
{
    public class UsernameHistoryService : IMService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;

        public UsernameHistoryService(DiscordSocketClient client, DbService db)
        {
            _db = db;
            _client = client;
            _log = LogManager.GetCurrentClassLogger();
            _client.UserJoined += UserJoined;
            _client.GuildMemberUpdated += UserUpdated;
            var _ = Task.Run(async () => await UpdateUsernames().ConfigureAwait(false));
        }

        public async Task<(int Nicks, int Usernames, int Users, TimeSpan Time)> UpdateUsernames()
        {
            var nickupdates = 0;
            var usernameupdates = 0;
            var time1 = DateTime.UtcNow;
            var users = _client.Guilds.Select(g => g.Users).Aggregate((u, g) => new List<SocketGuildUser>(u).Concat(g).ToArray());
            var usernicks = users.GroupBy(u => u.Id, u => new {
                GuildId = u.Guild.Id,
                u.Nickname
            }).ToList();
            var usernames = users.GroupBy(u => u.Id).Select(u => new {
                u.First().Id,
                u.First().Username,
                u.First().DiscriminatorValue
            }).ToList();
            using (var uow = _db.UnitOfWork)
            {
                usernameupdates += usernames.Count(u => uow.UsernameHistory.AddUsername(u.Id, u.Username, u.DiscriminatorValue));
                nickupdates += usernicks.Sum(nicks => nicks.Count(a => uow.NicknameHistory.AddUsername(a.GuildId, nicks.Key, a.Nickname, usernames.First(an => an.Id == nicks.Key).DiscriminatorValue)));
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            var ts = DateTime.UtcNow - time1;
            _log.Info($"Updated {nickupdates} nicknames and {usernameupdates} usernames for {usernicks.Count} users in {ts.TotalSeconds:F2}s");
            return (nickupdates, usernameupdates, usernicks.Count, ts);
        }

        private Task UserJoined(SocketGuildUser user)
        {
            var _ = Task.Run(async () => {
                using (var uow = _db.UnitOfWork)
                {
                    if (IsGuildLoggingUsernames(user.Guild.Id))
                        uow.NicknameHistory.AddUsername(user.Guild.Id, user.Id, user.Nickname, user.DiscriminatorValue);

                    if (IsGuildLoggingUsernames())
                        uow.UsernameHistory.AddUsername(user.Id, user.Username, user.DiscriminatorValue);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            });
            return Task.CompletedTask;
        }

        private Task UserUpdated(SocketGuildUser b, SocketGuildUser a)
        {
            var _ = Task.Run(async () => {
                if (b.Id != a.Id) return;
                using (var uow = _db.UnitOfWork)
                {
                    //_log.Info($"guild | {IsGuildLoggingUsernames(a.Guild.Id)} | {a.Guild.Id} | {a.Id} | {a.Nickname} | {a.DiscriminatorValue}");
                    if (IsGuildLoggingUsernames(a.Guild.Id))
                        uow.NicknameHistory.AddUsername(a.Guild.Id, a.Id, a.Nickname, a.DiscriminatorValue);

                    //_log.Info($"global | {IsGuildLoggingUsernames()} | {a.Id} | {a.Username} | {a.DiscriminatorValue}");
                    if (IsGuildLoggingUsernames())
                        uow.UsernameHistory.AddUsername(a.Id, a.Username, a.DiscriminatorValue);

                    await uow.CompleteAsync().ConfigureAwait(false);
                }
            });
            return Task.CompletedTask;
        }

        public bool IsGuildLoggingUsernames(ulong guildId = 0)
        {
            bool globalLogging;
            bool? guildLogging = null;
            using (var uow = _db.UnitOfWork)
            {
                globalLogging = uow.BotConfig.GetOrCreate().LogUsernames;
                if (guildId != 0)
                    guildLogging = uow.GuildConfigs.For(guildId, set => set)?.LogUsernameHistory;
            }

            return guildLogging ?? globalLogging;
        }
    }
}
