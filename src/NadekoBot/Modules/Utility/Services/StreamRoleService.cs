using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Mitternacht.Common.TypeReaders;
using Mitternacht.Extensions;
using Mitternacht.Modules.Utility.Common;
using Mitternacht.Modules.Utility.Common.Exceptions;
using Mitternacht.Modules.Utility.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;

namespace Mitternacht.Modules.Utility.Services
{
    public class StreamRoleService : IMService
    {
        private readonly DbService _db;
        private readonly ConcurrentDictionary<ulong, StreamRoleSettings> _guildSettings;
        private readonly Logger _log;

        public StreamRoleService(DiscordSocketClient client, DbService db, IEnumerable<GuildConfig> gcs)
        {
            _db = db;
            _log = LogManager.GetCurrentClassLogger();

            _guildSettings = gcs.ToDictionary(x => x.GuildId, x => x.StreamRole)
                .Where(x => x.Value != null && x.Value.Enabled)
                .ToConcurrent();

            client.GuildMemberUpdated += Client_GuildMemberUpdated;

            var _ = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(client.Guilds.Select(RescanUsers)).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            });
        }

        private Task Client_GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var _ = Task.Run(async () =>
            {
                //if user wasn't streaming or didn't have a game status at all
                if (_guildSettings.TryGetValue(after.Guild.Id, out var setting))
                {
                    await RescanUser(after, setting).ConfigureAwait(false);
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds or removes a user from a blacklist or a whitelist in the specified guild.
        /// </summary>
        /// <param name="listType"></param>
        /// <param name="guild">Guild</param>
        /// <param name="action">Add or rem action</param>
        /// <param name="userId">User's Id</param>
        /// <param name="userName">User's name#discrim</param>
        /// <returns>Whether the operation was successful</returns>
        public async Task<bool> ApplyListAction(StreamRoleListType listType, IGuild guild, AddRemove action, ulong userId, string userName)
        {
            userName.ThrowIfNull(nameof(userName));

            bool success;
            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(guild.Id);

                if (listType == StreamRoleListType.Whitelist) {
                    var userObj = new StreamRoleWhitelistedUser {
                        UserId = userId,
                        Username = userName,
                    };

                    success = action == AddRemove.Rem ? streamRoleSettings.Whitelist.Remove(userObj) : streamRoleSettings.Whitelist.Add(userObj);
                }
                else {
                    var userObj = new StreamRoleBlacklistedUser {
                        UserId = userId,
                        Username = userName,
                    };

                    success = action == AddRemove.Rem ? streamRoleSettings.Blacklist.Remove(userObj) : streamRoleSettings.Blacklist.Add(userObj);
                }

                await uow.CompleteAsync().ConfigureAwait(false);
                UpdateCache(guild.Id, streamRoleSettings);
            }
            if (success)
            {
                await RescanUsers(guild).ConfigureAwait(false);
            }
            return success;
        }

        /// <summary>
        /// Sets keyword on a guild and updates the cache.
        /// </summary>
        /// <param name="guild">Guild Id</param>
        /// <param name="keyword">Keyword to set</param>
        /// <returns>The keyword set</returns>
        public async Task<string> SetKeyword(IGuild guild, string keyword)
        {
            keyword = keyword?.Trim()?.ToLowerInvariant();

            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(guild.Id);

                streamRoleSettings.Keyword = keyword;
                UpdateCache(guild.Id, streamRoleSettings);
                uow.Complete();
            }

            await RescanUsers(guild).ConfigureAwait(false);
            return keyword;
        }

        /// <summary>
        /// Gets the currently set keyword on a guild.
        /// </summary>
        /// <param name="guildId">Guild Id</param>
        /// <returns>The keyword set</returns>
        public string GetKeyword(ulong guildId)
        {
            if (_guildSettings.TryGetValue(guildId, out var outSetting))
                return outSetting.Keyword;

            StreamRoleSettings setting;
            using (var uow = _db.UnitOfWork)
            {
                setting = uow.GuildConfigs.GetStreamRoleSettings(guildId);
            }

            UpdateCache(guildId, setting);

            return setting.Keyword;
        }

        /// <summary>
        /// Sets the role to monitor, and a role to which to add to 
        /// the user who starts streaming in the monitored role.
        /// </summary>
        /// <param name="fromRole">Role to monitor</param>
        /// <param name="addRole">Role to add to the user</param>
        public async Task SetStreamRole(IRole fromRole, IRole addRole)
        {
            fromRole.ThrowIfNull(nameof(fromRole));
            addRole.ThrowIfNull(nameof(addRole));

            StreamRoleSettings setting;
            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(fromRole.Guild.Id);

                streamRoleSettings.Enabled = true;
                streamRoleSettings.AddRoleId = addRole.Id;
                streamRoleSettings.FromRoleId = fromRole.Id;

                setting = streamRoleSettings;
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            UpdateCache(fromRole.Guild.Id, setting);

            foreach (var usr in await fromRole.GetMembersAsync())
            {
                if (usr is IGuildUser x)
                    await RescanUser(x, setting, addRole).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops the stream role feature on the specified guild.
        /// </summary>
        /// <param name="guild">Guild</param>
        /// <param name="cleanup"></param>
        public async Task StopStreamRole(IGuild guild, bool cleanup = false)
        {
            using (var uow = _db.UnitOfWork)
            {
                var streamRoleSettings = uow.GuildConfigs.GetStreamRoleSettings(guild.Id);
                streamRoleSettings.Enabled = false;
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (_guildSettings.TryRemove(guild.Id, out var _) && cleanup)
                await RescanUsers(guild).ConfigureAwait(false);
        }
        //todo multiple rescans at the same time?
        private async Task RescanUser(IGuildUser user, StreamRoleSettings setting, IRole addRole = null)
        {
            if (user.Activity != null &&
                    user.Activity.Type == ActivityType.Streaming
                    && setting.Enabled
                    && setting.Blacklist.All(x => x.UserId != user.Id)
                    && user.RoleIds.Contains(setting.FromRoleId)
                    && (string.IsNullOrWhiteSpace(setting.Keyword)
                        || user.Activity.Name.ToLowerInvariant().Contains(setting.Keyword.ToLowerInvariant())
                        || setting.Whitelist.Any(x => x.UserId == user.Id)))
            {
                try
                {
                    addRole = addRole ?? user.Guild.GetRole(setting.AddRoleId);
                    if (addRole == null)
                        throw new StreamRoleNotFoundException();

                    //check if he doesn't have addrole already, to avoid errors
                    if (!user.RoleIds.Contains(setting.AddRoleId))
                        await user.AddRoleAsync(addRole).ConfigureAwait(false);
                    _log.Info("Added stream role to user {0} in {1} server", user, user.Guild);
                }
                catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await StopStreamRole(user.Guild).ConfigureAwait(false);
                    _log.Warn("Error adding stream role(s). Forcibly disabling stream role feature.");
                    _log.Error(ex);
                    throw new StreamRolePermissionException();
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed adding stream role.");
                    _log.Error(ex);
                }
            }
            else
            {
                //check if user is in the addrole
                if (user.RoleIds.Contains(setting.AddRoleId))
                {
                    try
                    {
                        addRole = addRole ?? user.Guild.GetRole(setting.AddRoleId);
                        if (addRole == null)
                            throw new StreamRoleNotFoundException();

                        await user.RemoveRoleAsync(addRole).ConfigureAwait(false);
                        _log.Info("Removed stream role from a user {0} in {1} server", user, user.Guild);
                    }
                    catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        await StopStreamRole(user.Guild).ConfigureAwait(false);
                        _log.Warn("Error removing stream role(s). Forcibly disabling stream role feature.");
                        _log.Error(ex);
                        throw new StreamRolePermissionException();
                    }
                    _log.Info("Removed stream role from the user {0} in {1} server", user, user.Guild);
                }
            }
        }

        private async Task RescanUsers(IGuild guild)
        {
            if (!_guildSettings.TryGetValue(guild.Id, out var setting))
                return;

            var addRole = guild.GetRole(setting.AddRoleId);
            if (addRole == null)
                return;

            if (setting.Enabled)
            {
                var users = await guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);
                foreach (var usr in users.Where(x => x.RoleIds.Contains(setting.FromRoleId) || x.RoleIds.Contains(addRole.Id)))
                {
                    if(usr is IGuildUser x)
                        await RescanUser(x, setting, addRole).ConfigureAwait(false);
                }
            }
        }

        private void UpdateCache(ulong guildId, StreamRoleSettings setting)
        {
            _guildSettings.AddOrUpdate(guildId, (key) => setting, (key, old) => setting);
        }
    }
}
