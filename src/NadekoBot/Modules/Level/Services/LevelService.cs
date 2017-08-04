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
        public readonly DbService _db;
        public readonly CommandService _cmds;
        public readonly DiscordSocketClient _client;
        public LevelService(DiscordSocketClient client, DbService db, CommandService cmds)
        {
            _db = db;
            _client = client;
            client.MessageReceived += OnMessageReceived;
            client.MessageUpdated += OnMessageUpdated;
            client.MessageDeleted += OnMessageDeleted;
            client.MessageReceived += AddLevelRole;
        }

        public async Task AddLevelRole(SocketMessage sm)
        {
            var user = (IGuildUser) sm.Channel.GetUserAsync(sm.Author.Id);
            IEnumerable<IRole> rolesToAdd;
            using (var uow = _db.UnitOfWork) {
                var rlb = uow.RoleLevelBinding.GetAll().Where(rl => rl.MinimumLevel <= uow.LevelModel.GetLevel(user.Id) && !user.RoleIds.Contains(rl.RoleId));
                rolesToAdd = user.Guild.Roles.Where(r => rlb.FirstOrDefault(rl => rl.RoleId == r.Id) != null);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            if (rolesToAdd.Count() == 0)
                return;
            await user.AddRolesAsync(rolesToAdd).ConfigureAwait(false);
            var rolestring = "\"";
            foreach (var role in rolesToAdd) {
                rolestring += role.Name + "\", \"";
            }
            rolestring = rolestring.Substring(0, rolestring.Length - 3) + "\"";
            await sm.Channel.SendMessageAsync($"{user.Mention} hat die Rolle{(rolesToAdd.Count() > 1 ? "n" : "")} {rolestring} bekommen.");
        }

        public async Task OnMessageReceived(SocketMessage sm)
        {
            if (sm.Content.Length < 10 || sm.Author.IsBot)
                return;
            using (var uow = _db.UnitOfWork) {
                var time = DateTime.Now;
                if (uow.LevelModel.CanGetMessageXP(sm.Author.Id, time)) {
                    uow.LevelModel.TryAddXP(sm.Author.Id, sm.Content.Length > 25 ? 25 : sm.Content.Length, false);
                    uow.LevelModel.ReplaceTimestamp(sm.Author.Id, time);
                    await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(sm.Author.Id), sm.Author, sm.Channel);
                }
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> um, SocketMessage sm, ISocketMessageChannel smc)
        {
            if (!um.HasValue || um.Value.Author.IsBot || (um.Value.Content.Length > 25 && sm.Content.Length > 25) || (um.Value.Content.Length < 10 && sm.Content.Length < 10))
                return Task.CompletedTask;
            using (var uow = _db.UnitOfWork) {
                uow.LevelModel.TryAddXP(um.Value.Author.Id, sm.Content.Length - um.Value.Content.Length, false);
                await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(sm.Author.Id), sm.Author, smc);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        public async Task OnMessageDeleted(Cacheable<IMessage, ulong> um, ISocketMessageChannel smc)
        {
            if (!um.HasValue || um.Value.Author.IsBot || um.Value.Content.Length < 10 || _cmds.Commands.Any(c => um.Value.Content.StartsWith(c.Name + " ") || c.Aliases.Any(c2 => um.Value.Content.StartsWith(c2))))
                return Task.CompletedTask;
            using (var uow = _db.UnitOfWork) {
                uow.LevelModel.TryAddXP(um.Value.Author.Id, um.Value.Content.Length > 25 ? -25 : -um.Value.Content.Length);
                await SendLevelChangedMessage(uow.LevelModel.CalculateLevel(um.Value.Author.Id), um.Value.Author, smc);
                uow.Complete();
            }
        }

        public async Task SendLevelChangedMessage(CalculatedLevel cl, IUser user, ISocketMessageChannel smc)
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
