using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Repositories;

namespace NadekoBot.Modules.Level.Services
{
    public class LevelService : INService
    {
        private readonly DbService _db;
        private readonly CommandService _cmds;
        private readonly CommandHandler _ch;

        public LevelService(DiscordSocketClient client, DbService db, CommandService cmds, CommandHandler ch) {
            _db = db;
            _cmds = cmds;
            _ch = ch;
            client.MessageReceived += OnMessageReceived;
            client.MessageUpdated += OnMessageUpdated;
            client.MessageDeleted += OnMessageDeleted;
            client.MessageReceived += AddLevelRole;
        }

        private async Task AddLevelRole(SocketMessage sm)
        {
            if (!(sm.Author is IGuildUser user))
                return;
            if (sm.Author.IsBot ||
                _cmds.Commands.Any(
                    c => c.Aliases.Any(c2 => sm.Content.StartsWith(_ch.GetPrefix(user.Guild) + c2 + " ") || sm.Content.Equals(_ch.GetPrefix(user.Guild) + c2)))) return;
            
            List<IRole> rolesToAdd;
            using (var uow = _db.UnitOfWork) {
                var level = uow.LevelModel.GetLevel(user.Id);
                var userroles = user.GetRoles().ToList();
                var rlbs = uow.RoleLevelBinding.GetAll().ToList();
                if(!rlbs.Any()) return;
                var rlb = rlbs.Where(rl => rl.MinimumLevel <= level && userroles.All(ur => ur.Id != rl.RoleId)).ToList();
                rolesToAdd = user.Guild.Roles.Where(r => rlb.Any(rs => rs.RoleId == r.Id)).ToList();
            }
            if (!rolesToAdd.Any()) return;
            var rolestring = rolesToAdd.Aggregate("\"", (s, r) => $"{s}{r.Name}\", \"", s => s.Substring(0, s.Length - 3));
            await user.AddRolesAsync(rolesToAdd);
            await sm.Channel.SendMessageAsync($"{user.Mention} hat die Rolle{(rolesToAdd.Count > 1 ? "n" : "")} {rolestring} bekommen.");
        }

        private async Task OnMessageReceived(SocketMessage sm)
        {
            if (!(sm.Author is IGuildUser user))
                return;
            if (sm.Author.IsBot || sm.Content.Length < 10 ||
                _cmds.Commands.Any(
                    c => c.Aliases.Any(c2 => sm.Content.StartsWith(_ch.GetPrefix(user.Guild) + c2 + " ") || sm.Content.Equals(_ch.GetPrefix(user.Guild) + c2))))
                return;

            using (var uow = _db.UnitOfWork) {
                if (await uow.MessageXpBlacklist.IsRestrictedAsync(sm.Channel as ITextChannel))
                    return;
                var time = DateTime.Now;
                if (uow.LevelModel.CanGetMessageXp(user.Id, time)) {
                    uow.LevelModel.TryAddXp(user.Id, sm.Content.Length > 25 ? 25 : sm.Content.Length, false);
                    uow.LevelModel.ReplaceTimestamp(user.Id, time);
                    await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(user.Id), user, sm.Channel);
                }
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task OnMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            var msgBefore = await before.GetOrDownloadAsync();
            if (!(msgBefore.Author is IGuildUser user))
                return;

            if (msgBefore.Author.IsBot || msgBefore.Content.Length > 25 && after.Content.Length > 25 || msgBefore.Content.Length < 10 && after.Content.Length < 10 ||
                _cmds.Commands.Any(
                    c => c.Aliases.Any(c2 => msgBefore.Content.StartsWith(_ch.GetPrefix(user.Guild) + c2 + " ") || msgBefore.Content.Equals(_ch.GetPrefix(user.Guild) + c2))))
                return;

            using (var uow = _db.UnitOfWork) {
                if (await uow.MessageXpBlacklist.IsRestrictedAsync(channel as ITextChannel))
                    return;
                uow.LevelModel.TryAddXp(msgBefore.Author.Id, after.Content.Length - msgBefore.Content.Length, false);
                await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(after.Author.Id), after.Author, channel);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task OnMessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel channel)
        {
            var msgBefore = await before.GetOrDownloadAsync();
            if (!(msgBefore.Author is IGuildUser user))
                return;

            if (msgBefore.Author.IsBot || msgBefore.Content.Length < 10 ||
                _cmds.Commands.Any(
                    c => c.Aliases.Any(c2 => msgBefore.Content.StartsWith(_ch.GetPrefix(user.Guild) + c2 + " ") || msgBefore.Content.Equals(_ch.GetPrefix(user.Guild) + c2))))
                return;
            using (var uow = _db.UnitOfWork) {
                if (await uow.MessageXpBlacklist.IsRestrictedAsync(channel as ITextChannel))
                    return;
                uow.LevelModel.TryAddXp(msgBefore.Author.Id, msgBefore.Content.Length > 25 ? -25 : -msgBefore.Content.Length);
                await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(msgBefore.Author.Id), msgBefore.Author, channel);
                uow.Complete();
            }
        }

        public async Task SendLevelChangedMessage(CalculatedLevel cl, IMentionable user, IMessageChannel smc)
        {
            if (cl.IsNewLevelHigher) {
                await smc.SendMessageAsync($"Herzlichen Glückwunsch { user.Mention }, du bist von Level { cl.OldLevel } auf Level { cl.NewLevel } aufgestiegen!");
            }
            else if (cl.IsNewLevelLower) {
                await smc.SendMessageAsync($"Schade { user.Mention }, du bist von Level { cl.OldLevel } auf Level { cl.NewLevel } abgestiegen :(");
            }
        }
    }
}
