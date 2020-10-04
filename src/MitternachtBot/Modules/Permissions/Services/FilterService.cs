using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.ModuleBehaviors;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using Mitternacht.Services.Impl;
using NLog;

namespace Mitternacht.Modules.Permissions.Services {
	public class FilterService : IEarlyBlocker, IMService {
		private readonly Logger    _log;
		private readonly DbService _db;

		public FilterService(DiscordSocketClient client, DbService db) {
			_log = LogManager.GetCurrentClassLogger();
			_db = db;

			client.MessageUpdated += (oldData, newMsg, channel) => {
				var _ = Task.Run(() => {
					var guild = (channel as ITextChannel)?.Guild;

					return guild == null || !(newMsg is IUserMessage usrMsg) ? Task.CompletedTask : TryBlockEarly(guild, usrMsg);
				});
				return Task.CompletedTask;
			};
		}

		public async Task<bool> TryBlockEarly(IGuild guild, IUserMessage message, bool realExecution = true) {
			using var uow = _db.UnitOfWork;
			var gc = guild == null ? null : uow.GuildConfigs.For(guild.Id, set => set.Include(gc => gc.FilteredWords));
			return (!(guild is null) || !(message is null)) && message.Author is IGuildUser gu && !gu.GuildPermissions.ManageMessages && (await FilterInvites(guild, message, gc, realExecution) || await FilterWords(guild, message, gc, realExecution) || await FilterZalgo(guild, message, gc, realExecution));
		}

		private async Task<bool> FilterWords(IGuild guild, IUserMessage userMessage, GuildConfig gc, bool realExecution = true) {
			if(gc == null)
				return false;
			
			var filteredWords = gc.FilterWords || gc.FilterWordsChannelIds.Any(fwc => fwc.ChannelId == userMessage.Channel.Id) ? gc.FilteredWords.Select(fw => fw.Word).ToArray() : new string[0];

			if(filteredWords.Any(filteredWord => userMessage.Content.Contains(filteredWord, StringComparison.OrdinalIgnoreCase))) {
				if(realExecution) {
					try {
						await userMessage.DeleteAsync().ConfigureAwait(false);
					} catch(HttpException ex) {
						_log.Warn($"I do not have permission to filter words in channel with id {userMessage.Channel.Id}", ex);
					}
				}

				return true;
			} else {
				return false;
			}
		}

		private async Task<bool> FilterInvites(IGuild guild, IUserMessage userMessage, GuildConfig gc, bool realExecution = true) {
			if(gc == null)
				return false;
			
			if((gc.FilterInvites || gc.FilterInvitesChannelIds.Any(fic => fic.ChannelId == userMessage.Channel.Id)) && userMessage.Content.ContainsDiscordInvite()) {
				if(realExecution) {
					try {
						await userMessage.DeleteAsync().ConfigureAwait(false);
					} catch(HttpException ex) {
						_log.Warn($"I do not have permission to filter invites in channel with id {userMessage.Channel.Id}", ex);
					}
				}

				return true;
			} else {
				return false;
			}
		}

		private async Task<bool> FilterZalgo(IGuild guild, IUserMessage userMessage, GuildConfig gc, bool realExecution = true) {
			if(gc == null)
				return false;

			if((gc.FilterZalgo || gc.FilterZalgoChannelIds.Any(fzc => fzc.ChannelId == userMessage.Channel.Id)) && IsZalgo(userMessage.Content)) {
				if(realExecution) {
					try {
						await userMessage.DeleteAsync().ConfigureAwait(false);
						return true;
					} catch(HttpException e) {
						_log.Warn("I do not have permission to filter zalgo in channel with id " + userMessage.Channel.Id, e);
						return true;
					}
				} else {
					return true;
				}
			}

			return false;
		}

		private bool IsZalgo(string s) {
			if(string.IsNullOrWhiteSpace(s))
				return false;
			var scores = (from word in s.Split(' ')
						  let categories = word.Select(CharUnicodeInfo.GetUnicodeCategory).ToList()
						  select categories.Count(c => c == UnicodeCategory.EnclosingMark || c == UnicodeCategory.NonSpacingMark) / (word.Length * 1d))
						  .OrderBy(d => d).ToList();
			var k = (scores.Count - 1) * 0.75;
			var floor = (int) Math.Floor(k);
			var ceiling = (int) Math.Ceiling(k);
			double percentile = floor == ceiling ? scores[floor] : scores[floor] * (ceiling - k) + scores[ceiling] * (k - floor);
			return percentile > 0.5;
		}
	}
}