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

        public Task AddLevelRole(SocketMessage sm)
        {
            if(sm.Content.Equals(".die"))
                return Task.CompletedTask;

            var user = (IGuildUser)sm.Author;
            sm.Channel.SendMessageAsync("User: " + user.Username);
            IEnumerable<IRole> rolesToAdd;
            using (var uow = _db.UnitOfWork)
            {
                var rlb = uow.RoleLevelBinding.GetAll().Where(rl => rl.MinimumLevel <= uow.LevelModel.GetLevel(user.Id) && !user.RoleIds.Contains(rl.RoleId));
                rolesToAdd = user.Guild.Roles.Where(r => rlb.FirstOrDefault(rl => rl.RoleId == r.Id) != null);
                uow.Complete();
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

        public Task OnMessageReceived(SocketMessage sm)
        {
            if (sm.Content.Length < 10 || sm.Author.IsBot) return Task.CompletedTask;
            using (var uow = _db.UnitOfWork)
            {
                var time = DateTime.Now;
                if (!uow.LevelModel.CanGetMessageXP(sm.Author.Id, time)) return Task.CompletedTask;
                uow.LevelModel.TryAddXP(sm.Author.Id, sm.Content.Length > 25 ? 25 : sm.Content.Length, false);
                uow.LevelModel.ReplaceTimestamp(sm.Author.Id, time);
                SendLevelChangedMessage(uow.LevelModel.CalculateLevel(sm.Author.Id), sm.Author, sm.Channel);
                uow.Complete();
            }
            return Task.CompletedTask;
        }

        public Task OnMessageUpdated(Cacheable<IMessage, ulong> um, SocketMessage sm, ISocketMessageChannel smc)
        {
            if (!um.HasValue || um.Value.Author.IsBot || (um.Value.Content.Length > 25 && sm.Content.Length > 25) || (um.Value.Content.Length < 10 && sm.Content.Length < 10)) return Task.CompletedTask;
            using (var uow = _db.UnitOfWork)
            {
                uow.LevelModel.TryAddXP(um.Value.Author.Id, sm.Content.Length - um.Value.Content.Length, false);
                SendLevelChangedMessage(uow.LevelModel.CalculateLevel(sm.Author.Id), sm.Author, smc);
                uow.Complete();
            }
            return Task.CompletedTask;
        }

        public Task OnMessageDeleted(Cacheable<IMessage, ulong> um, ISocketMessageChannel smc)
        {
            if (!um.HasValue || um.Value.Author.IsBot || um.Value.Content.Length < 10 || _cmds.Commands.Any(c => um.Value.Content.StartsWith(c.Name + " ") || c.Aliases.Any(c2 => um.Value.Content.StartsWith(c2)))) return Task.CompletedTask;
            using (var uow = _db.UnitOfWork)
            {
                uow.LevelModel.TryAddXP(um.Value.Author.Id, um.Value.Content.Length > 25 ? -25 : -um.Value.Content.Length);
                SendLevelChangedMessage(uow.LevelModel.CalculateLevel(um.Value.Author.Id), um.Value.Author, smc);
                uow.Complete();
            }
            return Task.CompletedTask;
        }

        public void SendLevelChangedMessage(CalculatedLevel cl, IUser user, ISocketMessageChannel smc)
        {
            if (cl.IsNewLevelHigher)
            {
                smc.SendMessageAsync($"Herzlichen Glückwunsch { user.Mention }, du bist von Level { cl.OldLevel } auf Level { cl.NewLevel } aufgestiegen!");
            }
            else if (cl.IsNewLevelLower)
            {
                smc.SendMessageAsync($"Schade { user.Mention }, du bist von Level { cl.OldLevel } auf Level { cl.NewLevel } abgestiegen :(");
            }
        }
    }
}
