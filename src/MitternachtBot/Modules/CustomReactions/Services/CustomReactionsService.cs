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
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using MoreLinq;
using NLog;

namespace Mitternacht.Modules.CustomReactions.Services {
	public class CustomReactionsService : IEarlyBlockingExecutor, IMService {
		public CustomReaction[] GlobalReactions {
			get {
				using var uow = _db.UnitOfWork;
				return uow.CustomReactions.GetAll().Where(g => g.GuildId == null || g.GuildId == 0).ToArray();
			}
		}

		public ConcurrentDictionary<string, uint> ReactionStats { get; } = new ConcurrentDictionary<string, uint>();

		private readonly Logger _log = LogManager.GetCurrentClassLogger();

		private readonly DiscordSocketClient _client;
		private readonly PermissionService   _perms;
		private readonly CommandHandler      _cmd;
		private readonly IBotConfigProvider  _bc;
		private readonly StringService       _strings;
		private readonly DbService           _db;

		public CustomReactionsService(DiscordSocketClient client, PermissionService perms, CommandHandler cmd, IBotConfigProvider bc, StringService strings, DbService db) {
			_client  = client;
			_perms   = perms;
			_cmd     = cmd;
			_bc      = bc;
			_strings = strings;
			_db      = db;
		}

		public bool ClearStats(string trigger = null) {
			if(string.IsNullOrWhiteSpace(trigger)) {
				ReactionStats.Clear();
				return true;
			} else {
				return ReactionStats.TryRemove(trigger, out _);
			}
		}

		public CustomReaction[] ReactionsForGuild(ulong guildId) {
			using var uow = _db.UnitOfWork;
			return uow.CustomReactions.GetAll().Where(cr => cr.GuildId == guildId).ToArray();
		}

		public CustomReaction TryGetCustomReaction(IUserMessage umsg) {
			if(umsg.Channel is SocketTextChannel channel) {
				var content = umsg.Content.Trim();
				var reactions = ReactionsForGuild(channel.Guild.Id);

				if(reactions.Any()) {
					var rs = reactions.Where(cr => {
						var trigger = cr.TriggerWithContext(umsg, _client).Trim();
						return cr.ContainsAnywhere && content.GetWordPosition(trigger) != WordPosition.None
						   || cr.Response.Contains("%target%", StringComparison.OrdinalIgnoreCase) && content.StartsWith($"{trigger} ", StringComparison.OrdinalIgnoreCase)
						   || _bc.BotConfig.CustomReactionsStartWith && content.StartsWith($"{trigger} ", StringComparison.OrdinalIgnoreCase)
						   || content.Equals(trigger, StringComparison.OrdinalIgnoreCase);
					}).ToArray();

					if(rs.Any()) {
						var reaction = rs.RandomSubset(1).First();
						return reaction.Response == "-" ? null : reaction;
					}
				}

				var grs = GlobalReactions.Where(cr => {
					var hasTarget = cr.Response.Contains("%target%", StringComparison.OrdinalIgnoreCase);
					var trigger = cr.TriggerWithContext(umsg, _client).Trim();
					return hasTarget && content.StartsWith($"{trigger} ", StringComparison.OrdinalIgnoreCase) || _bc.BotConfig.CustomReactionsStartWith && content.StartsWith($"{trigger} ", StringComparison.OrdinalIgnoreCase) || content.Equals(trigger, StringComparison.OrdinalIgnoreCase);
				}).ToArray();

				return grs.Any() ? grs.RandomSubset(1).First() : null;
			} else {
				return null;
			}
		}

		public async Task<bool> TryExecuteEarly(DiscordSocketClient client, IGuild guild, IUserMessage msg, bool realExecution = true) {
			var cr = TryGetCustomReaction(msg);
			if(cr == null)
				return false;
			if(!realExecution)
				return true;

			try {
				if(guild is SocketGuild sg) {
					var pc = _perms.GetCache(guild.Id);
					if(!pc.Permissions.CheckPermissions(msg, cr.Trigger, "ActualCustomReactions", out var index)) {
						if(!pc.Verbose)
							return true;
						var returnMsg = _strings.GetText("permissions", "trigger", guild.Id, index + 1, Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), sg)));
						try { await msg.Channel.SendErrorAsync(returnMsg).ConfigureAwait(false); } catch { }
						_log.Info(returnMsg);
						return true;
					}
				}
				await cr.Send(msg, _client, this).ConfigureAwait(false);

				if(!cr.AutoDeleteTrigger)
					return true;
				try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
				return true;
			} catch(Exception ex) {
				_log.Warn("Sending CREmbed failed");
				_log.Warn(ex);
			}
			return false;
		}
	}
}