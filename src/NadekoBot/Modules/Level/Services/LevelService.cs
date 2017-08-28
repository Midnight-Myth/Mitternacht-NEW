using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Database.Repositories;

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

        private async Task AddLevelRole(SocketMessage sm)
        {
            if (sm.Content.Equals(".die") || sm.Author.IsBot) return;
            
            var user = sm.Author as IGuildUser;
            if (user == null) return;
            //sm.Channel.SendMessageAsync("User: " + user.Username);
            //sm.Channel.SendMessageAsync("Server: " + user.Guild);
            List<IRole> rolesToAdd;
            using (var uow = _db.UnitOfWork)
            {
                //sm.Channel.SendMessageAsync("1");
                var level = uow.LevelModel.GetLevel(user.Id);
                //sm.Channel.SendMessageAsync($"level: {level}");
                var userroles = user.GetRoles().ToList();
                //sm.Channel.SendMessageAsync($"userroles: {userroles.Aggregate("", (s, r) => $"{s}{r.Name}+{r.Id},").TrimEnd(',')}");
                var rlbs = uow.RoleLevelBinding.GetAll().ToList();
                if(!rlbs.Any()) return;
                //await sm.Channel.SendMessageAsync($"rlbs: {rlbs.Aggregate("", (s, r) => $"{s}{r.RoleId}+{r.MinimumLevel},").TrimEnd(',')}");
                var rlb = rlbs.Where(rl => rl.MinimumLevel <= level && userroles.All(ur => ur.Id != rl.RoleId)).ToList();
                //await sm.Channel.SendMessageAsync($"rlb count: {rlb.Count}");
                rolesToAdd = user.Guild.Roles.Where(r => rlb.Any(rs => rs.RoleId == r.Id)).ToList();
                //await sm.Channel.SendMessageAsync($"rolestoadd count: {rolesToAdd.Count}");
            }
            if (!rolesToAdd.Any()) return;
            var rolestring = rolesToAdd.Aggregate("\"", (s, r) => $"{s}{r.Name}\", \"").TrimEnd('"', ' ', ',');
            //await sm.Channel.SendMessageAsync($"Rollen: {rolestring}");
            await user.AddRolesAsync(rolesToAdd);
            await sm.Channel.SendMessageAsync($"{user.Mention} hat die Rolle{(rolesToAdd.Count > 1 ? "n" : "")} {rolestring} bekommen.");
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
