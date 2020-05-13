using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Modules.CustomReactions.Extensions;
using Mitternacht.Modules.Permissions.Common;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.CustomReactions.Services
{
    public class CustomReactionsService : IEarlyBlockingExecutor, IMService
    {
        public CustomReaction[] GlobalReactions;
        public ConcurrentDictionary<ulong, CustomReaction[]> GuildReactions { get; }

        public ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

        private readonly Logger _log;
        private readonly DiscordSocketClient _client;
        private readonly PermissionService _perms;
        private readonly CommandHandler _cmd;
        private readonly IBotConfigProvider _bc;
        private readonly StringService _strings;

        public CustomReactionsService(PermissionService perms, StringService strings, DiscordSocketClient client, CommandHandler cmd, IBotConfigProvider bc, IUnitOfWork uow)
        {
            _log = LogManager.GetCurrentClassLogger();
            _client = client;
            _perms = perms;
            _cmd = cmd;
            _bc = bc;
            _strings = strings;

            var items = uow.CustomReactions.GetAll().ToList();
            GuildReactions = new ConcurrentDictionary<ulong, CustomReaction[]>(items.Where(cr => cr.GuildId != null && cr.GuildId != 0).GroupBy(cr => cr.GuildId.Value).ToDictionary(cr => cr.Key, cr => cr.ToArray()));
            GlobalReactions = items.Where(g => g.GuildId == null || g.GuildId == 0).ToArray();
        }

        public void ClearStats() => ReactionStats.Clear();

        public CustomReaction TryGetCustomReaction(IUserMessage umsg)
        {
            if (!(umsg.Channel is SocketTextChannel channel)) return null;

            var content = umsg.Content.Trim().ToLowerInvariant();

            if (GuildReactions.TryGetValue(channel.Guild.Id, out var reactions) && reactions != null && reactions.Any()) {
                var rs = reactions.Where(cr => {
                    if (cr == null) return false;

                    var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                    var trigger = cr.TriggerWithContext(umsg, _client).Trim().ToLowerInvariant();
                    return cr.ContainsAnywhere && content.GetWordPosition(trigger) != WordPosition.None
                           || hasTarget && content.StartsWith(trigger + " ")
                           || _bc.BotConfig.CustomReactionsStartWith && content.StartsWith(trigger + " ")
                           || content == trigger;
                }).ToArray();

                if (rs.Length != 0) {
                    var reaction = rs[new NadekoRandom().Next(0, rs.Length)];
                    if (reaction != null) {
                        return reaction.Response == "-" ? null : reaction;
                    }
                }
            }

            var grs = GlobalReactions.Where(cr =>
            {
                if (cr == null)
                    return false;
                var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                var trigger = cr.TriggerWithContext(umsg, _client).Trim().ToLowerInvariant();
                return hasTarget && content.StartsWith(trigger + " ") || _bc.BotConfig.CustomReactionsStartWith && content.StartsWith(trigger + " ") || content == trigger;
            }).ToArray();
            if (grs.Length == 0)
                return null;
            var greaction = grs[new NadekoRandom().Next(0, grs.Length)];

            return greaction;
        }

        public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage msg, bool realExecution = true)
        {
            // maybe this message is a custom reaction
            var cr = await Task.Run(() => TryGetCustomReaction(msg)).ConfigureAwait(false);
            if (cr == null) return false;
            if (!realExecution) return true;

            try
            {
                if (guild is SocketGuild sg)
                {
                    var pc = _perms.GetCache(guild.Id);
                    if (!pc.Permissions.CheckPermissions(msg, cr.Trigger, "ActualCustomReactions", out var index))
                    {
                        if (!pc.Verbose) return true;
                        var returnMsg = _strings.GetText("Permissions".ToLowerInvariant(), "trigger", guild.Id, index + 1, Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), sg)));
                        try { await msg.Channel.SendErrorAsync(returnMsg).ConfigureAwait(false); } catch { /*ignored*/ }
                        _log.Info(returnMsg);
                        return true;
                    }
                }
                await cr.Send(msg, _client, this).ConfigureAwait(false);

                if (!cr.AutoDeleteTrigger) return true;
                try { await msg.DeleteAsync().ConfigureAwait(false); } catch { /*ignored*/ }
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn("Sending CREmbed failed");
                _log.Warn(ex);
            }
            return false;
        }
    }
}