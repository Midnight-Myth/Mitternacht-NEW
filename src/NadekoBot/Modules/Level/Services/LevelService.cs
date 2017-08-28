using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Services.Database.Repositories;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace NadekoBot.Modules.Level.Services
{
    public class LevelService : INService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        private readonly CommandService _cmds;
        public LevelService(DiscordSocketClient client, DbService db, CommandService cmds) {
            _client = client;
            _db = db;
            _cmds = cmds;
            client.MessageReceived += OnMessageReceived;
            client.MessageUpdated += OnMessageUpdated;
            client.MessageDeleted += OnMessageDeleted;
            client.MessageReceived += AddLevelRole;
        }

        public Task AddLevelRole(SocketMessage sm)
        {
            if (sm.Content.Equals(".die"))
                return Task.CompletedTask;

            if (sm.Author.IsBot) return Task.CompletedTask;

            var user = (IGuildUser)sm.Author;
            sm.Channel.SendMessageAsync("User: " + user.Username);
            IEnumerable<IRole> rolesToAdd;
            using (var uow = _db.UnitOfWork)
            {
                sm.Channel.SendMessageAsync("1");
                var rlb = uow.RoleLevelBinding.GetAll().Where(rl => rl.MinimumLevel <= uow.LevelModel.GetLevel(user.Id) && !user.RoleIds.Contains(rl.RoleId));
                sm.Channel.SendMessageAsync("2");
                rolesToAdd = user.Guild.Roles.Where(r => rlb.FirstOrDefault(rl => rl.RoleId == r.Id) != null);
                sm.Channel.SendMessageAsync("3");
                uow.Complete();
                sm.Channel.SendMessageAsync("4");
            }
            sm.Channel.SendMessageAsync("Anzahl Rollen: " + rolesToAdd.Count());
            if (rolesToAdd.Count() == 0) return Task.CompletedTask;
            sm.Channel.SendMessageAsync("Rollen: " + rolesToAdd);
            user.AddRolesAsync(rolesToAdd).ConfigureAwait(false);
            var rolestring = "\"";
            foreach (var role in rolesToAdd)
            {
                rolestring += role.Name + "\", \"";
            }
            rolestring = rolestring.Substring(0, rolestring.Length - 3) + "\"";
            sm.Channel.SendMessageAsync($"{user.Mention} hat die Rolle{(rolesToAdd.Count() > 1 ? "n" : "")} {rolestring} bekommen.");
            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(SocketMessage after)
        {
            if (after.Content.Length < 10 || after.Author.IsBot)
                return;
            using (var uow = _db.UnitOfWork) {
                var time = DateTime.Now;
                if (uow.LevelModel.CanGetMessageXP(after.Author.Id, time)) {
                    uow.LevelModel.TryAddXP(after.Author.Id, after.Content.Length > 25 ? 25 : after.Content.Length, false);
                    uow.LevelModel.ReplaceTimestamp(after.Author.Id, time);
                    await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(after.Author.Id), after.Author, after.Channel);
                }
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task OnMessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            var msgBefore = await before.GetOrDownloadAsync();
            if (msgBefore.Author.IsBot || (msgBefore.Content.Length > 25 && after.Content.Length > 25) || (msgBefore.Content.Length < 10 && after.Content.Length < 10))
                return;
            using (var uow = _db.UnitOfWork) {
                uow.LevelModel.TryAddXP(msgBefore.Author.Id, after.Content.Length - msgBefore.Content.Length, false);
                await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(after.Author.Id), after.Author, channel);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private async Task OnMessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel channel)
        {
            var msgBefore = await before.GetOrDownloadAsync();
            if (msgBefore.Author.IsBot || msgBefore.Content.Length < 10 || _cmds.Commands.Any(c => msgBefore.Content.StartsWith(c.Name + " ") || c.Aliases.Any(c2 => msgBefore.Content.StartsWith(c2))))
                return;
            using (var uow = _db.UnitOfWork) {
                uow.LevelModel.TryAddXP(msgBefore.Author.Id, msgBefore.Content.Length > 25 ? -25 : -msgBefore.Content.Length);
                await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(msgBefore.Author.Id), msgBefore.Author, channel);
                uow.Complete();
            }
        }

        private async Task SendLevelChangedMessage(CalculatedLevel cl, IUser user, ISocketMessageChannel smc)
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
