using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Administration.Services
{
    public class VcRoleService : IMService
    {
        private readonly Logger _log;

        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>> VcRoles { get; }

        public VcRoleService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs, DbService db)
        {
            _log = LogManager.GetCurrentClassLogger();
            client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
            VcRoles = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, IRole>>();
            var missingRoles = new List<VcRoleInfo>();
            foreach (var gconf in gcs)
            {
                var g = client.GetGuild(gconf.GuildId);
                if (g == null)
                    continue;

                var infos = new ConcurrentDictionary<ulong, IRole>();
                VcRoles.TryAdd(gconf.GuildId, infos);
                foreach (var ri in gconf.VcRoleInfos)
                {
                    var role = g.GetRole(ri.RoleId);
                    if (role == null)
                    {
                        missingRoles.Add(ri);
                        continue;
                    }

                    infos.TryAdd(ri.VoiceChannelId, role);
                }
            }
            if (!missingRoles.Any()) return;
            using (var uow = db.UnitOfWork)
            {
                _log.Warn($"Removing {missingRoles.Count} missing roles from {nameof(VcRoleService)}");
                uow.Context.RemoveRange(missingRoles);
                uow.Complete();
            }
        }

        private Task ClientOnUserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState,
            SocketVoiceState newState)
        {
            if (!(usr is SocketGuildUser gusr))
                return Task.CompletedTask;

            var oldVc = oldState.VoiceChannel;
            var newVc = newState.VoiceChannel;
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (oldVc != newVc)
                    {
                        ulong guildId;
                        guildId = newVc?.Guild.Id ?? oldVc.Guild.Id;

                        if (VcRoles.TryGetValue(guildId, out var guildVcRoles))
                        {
                            //remove old
                            if (oldVc != null && guildVcRoles.TryGetValue(oldVc.Id, out var role))
                            {
                                if (gusr.Roles.Contains(role))
                                {
                                    try
                                    {
                                        await gusr.RemoveRoleAsync(role).ConfigureAwait(false);
                                        await Task.Delay(500).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        await Task.Delay(200).ConfigureAwait(false);
                                        await gusr.RemoveRoleAsync(role).ConfigureAwait(false);
                                        await Task.Delay(500).ConfigureAwait(false);
                                    }
                                }
                            }
                            //add new
                            if (newVc != null && guildVcRoles.TryGetValue(newVc.Id, out role))
                            {
                                if (!gusr.Roles.Contains(role))
                                    await gusr.AddRoleAsync(role).ConfigureAwait(false);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            });
            return Task.CompletedTask;
        }
    }
}
