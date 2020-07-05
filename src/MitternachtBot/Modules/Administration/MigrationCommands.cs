using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Data.Sqlite;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Modules.Administration.Common.Migration;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
using Newtonsoft.Json;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MigrationCommands : MitternachtSubmodule
        {
            private const int CurrentVersion = 1;
            private readonly IUnitOfWork uow;

            public MigrationCommands(IUnitOfWork uow)
            {
                this.uow = uow;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task MigrateData()
            {
                var version = uow.BotConfig.GetOrCreate().MigrationVersion;

                try
                {
                    for (var i = version; i < CurrentVersion; i++) {
                        if (i == 0) Migrate0_9To1_0();
                    }
                    await ReplyConfirmLocalized("migration_done").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    await ReplyErrorLocalized("migration_error").ConfigureAwait(false);
                }
            }

            private void Migrate0_9To1_0()
            {
                var botConfig = uow.BotConfig.GetOrCreate();
                MigrateConfig0_9(uow, botConfig);
                MigratePermissions0_9(uow);
                MigrateServerSpecificConfigs0_9(uow);
                MigrateDb0_9(uow);

                //NOW save it
                _log.Warn("Writing to disc");
                uow.SaveChanges(false);
                botConfig.MigrationVersion = 1;
            }

            private void MigrateDb0_9(IUnitOfWork uow)
            {
                var db = new SqliteConnection("Data Source=data/nadekobot.sqlite");

                if (!File.Exists("data/nadekobot.sqlite"))
                {
                    _log.Warn("No data from the old database will be migrated.");
                    return;
                }
                db.Open();

                var com = db.CreateCommand();
                var i = 0;
                try
                {
                    com.CommandText = "SELECT * FROM Announcement";

                    var reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        var gid = (ulong)(long)reader["ServerId"];
                        var greet = (long)reader["Greet"] == 1;
                        var greetDm = (long)reader["GreetPM"] == 1;
                        var greetChannel = (ulong)(long)reader["GreetChannelId"];
                        var greetMsg = (string)reader["GreetText"];
                        var bye = (long)reader["Bye"] == 1;
                        var byeChannel = (ulong)(long)reader["ByeChannelId"];
                        var byeMsg = (string)reader["ByeText"];
                        var gc = uow.GuildConfigs.For(gid);

                        if (greetDm)
                            gc.SendDmGreetMessage = greet;
                        else
                            gc.SendChannelGreetMessage = greet;
                        gc.GreetMessageChannelId = greetChannel;
                        gc.ChannelGreetMessageText = greetMsg;

                        gc.SendChannelByeMessage = bye;
                        gc.ByeMessageChannelId = byeChannel;
                        gc.ChannelByeMessageText = byeMsg;

                        _log.Info(++i);
                    }
                }
                catch
                {
                    _log.Warn("Greet/bye messages won't be migrated");
                }
                var com2 = db.CreateCommand();
                com2.CommandText = "SELECT * FROM CurrencyState GROUP BY UserId";

                i = 0;
                try
                {
                    var reader2 = com2.ExecuteReader();
                    while (reader2.Read())
                    {
                        _log.Info(++i);
                        var curr = new Currency {
                            Amount = (long)reader2["Value"],
                            UserId = (ulong)(long)reader2["UserId"]
                        };
                        uow.Currency.Add(curr);
                    }
                }
                catch
                {
                    _log.Warn("Currency won't be migrated");
                }
                db.Close();
                try { File.Move("data/nadekobot.sqlite", "data/DELETE_ME_nadekobot.sqlite"); } catch { /*ignored*/ }
            }

            private void MigrateServerSpecificConfigs0_9(IUnitOfWork uow)
            {
                const string specificConfigsPath = "data/ServerSpecificConfigs.json";

                if (!File.Exists(specificConfigsPath))
                {
                    _log.Warn($"No data from {specificConfigsPath} will be migrated.");
                    return;
                }

                ConcurrentDictionary<ulong, ServerSpecificConfig> configs;
                try
                {
                    configs = JsonConvert
                        .DeserializeObject<ConcurrentDictionary<ulong, ServerSpecificConfig>>(
                            File.ReadAllText(specificConfigsPath), new JsonSerializerSettings()
                            {
                                Error = (s, e) =>
                                {
                                    if (e.ErrorContext.Member.ToString() == "GenerateCurrencyChannels")
                                    {
                                        e.ErrorContext.Handled = true;
                                    }
                                }
                            });
                }
                catch (Exception ex)
                {
                    _log.Warn(ex, "ServerSpecificConfig deserialization failed");
                    return;
                }
                var i = 0;
                var selfAssRoles = new ConcurrentHashSet<SelfAssignedRole>();
                configs
                    .Select(p => new { data = p.Value, gconfig = uow.GuildConfigs.For(p.Key, true) })
                    .AsParallel()
                    .ForAll(config =>
                    {
                        try
                        {
                            var guildConfig = config.gconfig;
                            var data = config.data;

                            guildConfig.AutoAssignRoleId = data.AutoAssignedRole;
                            guildConfig.DeleteMessageOnCommand = data.AutoDeleteMessagesOnCommand;
                            guildConfig.DefaultMusicVolume = data.DefaultMusicVolume;
                            guildConfig.ExclusiveSelfAssignedRoles = data.ExclusiveSelfAssignedRoles;
                            guildConfig.GenerateCurrencyChannelIds = new HashSet<GCChannelId>(data.GenerateCurrencyChannels.Select(gc => new GCChannelId { ChannelId = gc.Key }));
                            selfAssRoles.AddRange(data.ListOfSelfAssignableRoles.Select(r => new SelfAssignedRole { GuildId = guildConfig.GuildId, RoleId = r }).ToArray());
                            guildConfig.LogSetting.IgnoredChannels = new HashSet<IgnoredLogChannel>(data.LogserverIgnoreChannels.Select(id => new IgnoredLogChannel { ChannelId = id }));

                            guildConfig.LogSetting.LogUserPresenceId = data.LogPresenceChannel;


                            guildConfig.FollowedStreams = new HashSet<FollowedStream>(data.ObservingStreams.Select(x =>
                            {
                                var type = FollowedStream.FollowedStreamType.Twitch;
                                switch (x.Type)
                                {
                                    case StreamNotificationConfig0_9.StreamType.Twitch:
                                        type = FollowedStream.FollowedStreamType.Twitch;
                                        break;
                                    case StreamNotificationConfig0_9.StreamType.Beam:
                                        type = FollowedStream.FollowedStreamType.Mixer;
                                        break;
                                    case StreamNotificationConfig0_9.StreamType.Hitbox:
                                        type = FollowedStream.FollowedStreamType.Smashcast;
                                        break;
                                    case StreamNotificationConfig0_9.StreamType.YoutubeGaming:
                                        break;
                                }

                                return new FollowedStream {
                                    ChannelId = x.ChannelId,
                                    GuildId = guildConfig.GuildId,
                                    Username = x.Username.ToLowerInvariant(),
                                    Type = type
                                };
                            }));
                            guildConfig.VoicePlusTextEnabled = data.VoicePlusTextEnabled;
                            _log.Info("Migrating SpecificConfig for {0} done ({1})", guildConfig.GuildId, ++i);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }
                    });
                uow.SelfAssignedRoles.AddRange(selfAssRoles.ToArray());
                try { File.Move("data/ServerSpecificConfigs.json", "data/DELETE_ME_ServerSpecificCOnfigs.json"); } catch { /*ignored*/ }
            }

            private void MigratePermissions0_9(IUnitOfWork uow)
            {
                var permissionsDict = new ConcurrentDictionary<ulong, ServerPermissions0_9>();
                if (!Directory.Exists("data/permissions/"))
                {
                    _log.Warn("No data from permissions will be migrated.");
                    return;
                }
                foreach (var file in Directory.EnumerateFiles("data/permissions/"))
                {
                    try
                    {
                        var strippedFileName = Path.GetFileNameWithoutExtension(file);
                        if (string.IsNullOrWhiteSpace(strippedFileName)) continue;
                        var id = ulong.Parse(strippedFileName);
                        var data = JsonConvert.DeserializeObject<ServerPermissions0_9>(File.ReadAllText(file));
                        permissionsDict.TryAdd(id, data);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                var i = 0;
                permissionsDict
                    .Select(p => new { data = p.Value, gconfig = uow.GuildConfigs.For(p.Key, true) })
                    .AsParallel()
                    .ForAll(perms =>
                    {
                        try
                        {
                            var data = perms.data;
                            var gconfig = perms.gconfig;

                            gconfig.PermissionRole = data.PermissionsControllerRole;
                            gconfig.VerbosePermissions = data.Verbose;
                            gconfig.FilteredWords = new HashSet<FilteredWord>(data.Words.Select(w => w.ToLowerInvariant())
                                .Distinct()
                                .Select(w => new FilteredWord {
                                    Word = w
                                }));
                            gconfig.FilterWords = data.Permissions.FilterWords;
                            gconfig.FilterInvites = data.Permissions.FilterInvites;

                            gconfig.FilterInvitesChannelIds = new HashSet<FilterChannelId>();
                            gconfig.FilterInvitesChannelIds.AddRange(data.ChannelPermissions.Where(kvp => kvp.Value.FilterInvites)
                                .Select(cp => new FilterChannelId {
                                    ChannelId = cp.Key
                                }));

                            gconfig.FilterWordsChannelIds = new HashSet<FilterChannelId>();
                            gconfig.FilterWordsChannelIds.AddRange(data.ChannelPermissions.Where(kvp => kvp.Value.FilterWords)
                                .Select(cp => new FilterChannelId {
                                    ChannelId = cp.Key
                                }));

                            gconfig.CommandCooldowns = new HashSet<CommandCooldown>(data.CommandCooldowns
                                .Where(cc => !string.IsNullOrWhiteSpace(cc.Key) && cc.Value > 0)
                                .Select(cc => new CommandCooldown {
                                    CommandName = cc.Key,
                                    Seconds = cc.Value
                                }));
                            _log.Info("Migrating data from permissions folder for {0} done ({1})", gconfig.GuildId, ++i);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }
                    });

                try { Directory.Move("data/permissions", "data/DELETE_ME_permissions"); } catch { /*ignored*/ }

            }

            private void MigrateConfig0_9(IUnitOfWork uow, BotConfig botConfig)
            {
                Config0_9 oldConfig;
                const string configPath = "data/config.json";
                try
                {
                    oldConfig = JsonConvert.DeserializeObject<Config0_9>(File.ReadAllText(configPath));
                }
                catch (FileNotFoundException)
                {
                    _log.Warn("config.json not found");
                    return;
                }
                catch (Exception)
                {
                    _log.Error("Unknown error while deserializing file config.json, pls check its integrity, aborting migration");
                    throw new MigrationException();
                }

                //Basic
                botConfig.ForwardMessages = oldConfig.ForwardMessages;
                botConfig.ForwardToAllOwners = oldConfig.ForwardToAllOwners;
                botConfig.BufferSize = (ulong)oldConfig.BufferSize;
                botConfig.RemindMessageFormat = oldConfig.RemindMessageFormat;
                botConfig.CurrencySign = oldConfig.CurrencySign;
                botConfig.CurrencyName = oldConfig.CurrencyName;
                botConfig.DMHelpString = oldConfig.DMHelpString;
                botConfig.HelpString = oldConfig.HelpString;

                //messages
                botConfig.RotatingStatuses = oldConfig.IsRotatingStatus;
                var messages = new List<PlayingStatus>();

                oldConfig.RotatingStatuses.ForEach(i => messages.Add(new PlayingStatus { Status = i }));
                botConfig.RotatingStatusMessages = messages;

                //Blacklist
                var blacklist = new HashSet<BlacklistItem>(oldConfig.ServerBlacklist.Select(server => new BlacklistItem { ItemId = server, Type = BlacklistType.Server }));
                blacklist.AddRange(oldConfig.ChannelBlacklist.Select(channel => new BlacklistItem { ItemId = channel, Type = BlacklistType.Channel }));
                blacklist.AddRange(oldConfig.UserBlacklist.Select(user => new BlacklistItem { ItemId = user, Type = BlacklistType.User }));
                botConfig.Blacklist = blacklist;

                //Eightball
                botConfig.EightBallResponses = new HashSet<EightBallResponse>(oldConfig._8BallResponses.Select(response => new EightBallResponse { Text = response }));

                //customreactions
                uow.CustomReactions.AddRange(oldConfig.CustomReactions.SelectMany(cr =>
                {
                    return cr.Value.Select(res => new CustomReaction {
                        GuildId = null,
                        IsRegex = false,
                        OwnerOnly = false,
                        Response = res,
                        Trigger = cr.Key.ToLowerInvariant(),
                    });
                }).ToArray());

                try { File.Move(configPath, "./data/DELETE_ME_config.json"); } catch { /*ignored*/ }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ReplaceDefaultLevelModelGuild(ulong guildId) {
                var counter = 0;
                var lms = uow.LevelModel.GetAll().ToList();
                var overlapping = lms.Where(lm => lm.GuildId == 0)
                    .Concat(lms.Where(lm => lm.GuildId == guildId))
                    .GroupBy(lm => lm.UserId)
                    .Where(g => g.Any(lm => lm.GuildId == 0) && g.Any(lm => lm.GuildId == guildId));

                foreach (var ol in overlapping) {
                    var given = ol.First(lm => lm.GuildId == guildId);
                    var zero = ol.First(lm => lm.GuildId == 0);
                    zero.TotalXP += given.TotalXP;
                    uow.LevelModel.Update(zero);
                    uow.LevelModel.Remove(given);
                }

                foreach (var lm in uow.LevelModel.GetAll()) {
                    if (lm.GuildId != 0) continue;
                    lm.GuildId = guildId;
                    counter++;
                    uow.LevelModel.Update(lm);
                }
                await uow.SaveChangesAsync(false).ConfigureAwait(false);

                await ConfirmLocalized("replacedefaultlevelmodelguild", counter).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SetGuildConfigLevelDefaults(params ulong[] guildIds) {
                var gcs = uow.GuildConfigs.GetAll().ToList();
                if (guildIds.Length > 0) gcs = gcs.Where(gc => guildIds.Contains(gc.GuildId)).ToList();
                var count = gcs.Count;
                gcs.ForEach(gc => {
                    gc.TurnToXpMultiplier = 5;
                    gc.MessageXpTimeDifference = 60;
                    gc.MessageXpCharCountMin = 10;
                    gc.MessageXpCharCountMax = 25;
                });
                uow.GuildConfigs.UpdateRange(gcs.ToArray());
                await uow.SaveChangesAsync(false).ConfigureAwait(false);

                await ConfirmLocalized("setguildconfigleveldefaults", count).ConfigureAwait(false);
            }
        }
    }
}