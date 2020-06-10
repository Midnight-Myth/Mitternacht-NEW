using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Administration.Services
{
    public class VplusTService : IMService
    {
        private readonly Regex _channelNameRegex = new Regex(@"[^a-zA-Z0-9 -]", RegexOptions.Compiled);

        public readonly ConcurrentHashSet<ulong> VoicePlusTextCache;

        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLockObjects = new ConcurrentDictionary<ulong, SemaphoreSlim>();
        private readonly DiscordSocketClient _client;
        private readonly StringService _strings;
        private readonly DbService _db;
        private readonly Logger _log;

        public VplusTService(DiscordSocketClient client, IEnumerable<GuildConfig> gcs, StringService strings,
            DbService db)
        {
            _client = client;
            _strings = strings;
            _db = db;
            _log = LogManager.GetCurrentClassLogger();

            VoicePlusTextCache = new ConcurrentHashSet<ulong>(gcs.Where(g => g.VoicePlusTextEnabled).Select(g => g.GuildId));
            _client.UserVoiceStateUpdated += UserUpdatedEventHandler;
        }

        private Task UserUpdatedEventHandler(SocketUser iuser, SocketVoiceState before, SocketVoiceState after)
        {
            var user = (iuser as SocketGuildUser);
            var guild = user?.Guild;

            if (guild == null)
                return Task.CompletedTask;

            var botUserPerms = guild.CurrentUser.GuildPermissions;

            if (before.VoiceChannel == after.VoiceChannel)
                return Task.CompletedTask;

            if (!VoicePlusTextCache.Contains(guild.Id))
                return Task.CompletedTask;

            var _ = Task.Run(async () =>
            {
                try
                {

                    if (!botUserPerms.ManageChannels || !botUserPerms.ManageRoles)
                    {
                        try
                        {
                            await guild.Owner.SendErrorAsync(
                                _strings.GetText("Administration".ToLowerInvariant(),
									"vt_exit",
									guild.Id,
									Format.Bold(guild.Name))).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                        using (var uow = _db.UnitOfWork)
                        {
                            uow.GuildConfigs.For(guild.Id).VoicePlusTextEnabled = false;
                            VoicePlusTextCache.TryRemove(guild.Id);
                            await uow.CompleteAsync().ConfigureAwait(false);
                        }
                        return;
                    }

                    var semaphore = _guildLockObjects.GetOrAdd(guild.Id, (key) => new SemaphoreSlim(1, 1));

                    try
                    {
                        await semaphore.WaitAsync().ConfigureAwait(false);

                        var beforeVch = before.VoiceChannel;
                        if (beforeVch != null)
                        {
                            var beforeRoleName = GetRoleName(beforeVch);
                            var beforeRole = guild.Roles.FirstOrDefault(x => x.Name == beforeRoleName);
                            if (beforeRole != null)
                            {
                                _log.Info("Removing role " + beforeRoleName + " from user " + user.Username);
                                await user.RemoveRoleAsync(beforeRole).ConfigureAwait(false);
                                await Task.Delay(200).ConfigureAwait(false);
                            }
                        }
                        var afterVch = after.VoiceChannel;
                        if (afterVch != null && guild.AFKChannel?.Id != afterVch.Id)
                        {
                            var roleName = GetRoleName(afterVch);
                            var roleToAdd = guild.Roles.FirstOrDefault(x => x.Name == roleName) ??
                                              (IRole)await guild.CreateRoleAsync(roleName, GuildPermissions.None, isMentionable: false).ConfigureAwait(false);

                            ITextChannel textChannel = guild.TextChannels
                                                        .FirstOrDefault(t => t.Name == GetChannelName(afterVch.Name).ToLowerInvariant());
                            if (textChannel == null)
                            {
                                var created = (await guild.CreateTextChannelAsync(GetChannelName(afterVch.Name).ToLowerInvariant()).ConfigureAwait(false));

                                try { await guild.CurrentUser.AddRoleAsync(roleToAdd).ConfigureAwait(false); } catch {/*ignored*/}
                                await Task.Delay(50).ConfigureAwait(false);
                                await created.AddPermissionOverwriteAsync(roleToAdd, new OverwritePermissions(
                                    viewChannel: PermValue.Allow,
                                    sendMessages: PermValue.Allow))
                                        .ConfigureAwait(false);
                                await Task.Delay(50).ConfigureAwait(false);
                                await created.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(
                                    viewChannel: PermValue.Deny,
                                    sendMessages: PermValue.Deny))
                                        .ConfigureAwait(false);
                                await Task.Delay(50).ConfigureAwait(false);
                            }
                            _log.Info("Adding role " + roleToAdd.Name + " to user " + user.Username);
                            await user.AddRoleAsync(roleToAdd).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            });
            return Task.CompletedTask;
        }

        public string GetChannelName(string voiceName) =>
            _channelNameRegex.Replace(voiceName, "").Trim().Replace(" ", "-").TrimTo(90, true) + "-voice";

        public string GetRoleName(IVoiceChannel ch) =>
            "nvoice-" + ch.Id;
    }
}
