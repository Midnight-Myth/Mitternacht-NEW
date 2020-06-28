using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Collections;
using Mitternacht.Extensions;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions {
		[Group]
		public class FilterCommands : MitternachtSubmodule {
			private readonly DbService _db;
			private readonly FilterService _service;

			public FilterCommands(FilterService service, DbService db) {
				_service = service;
				_db = db;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task SrvrFilterInv() {
				var channel = (ITextChannel) Context.Channel;

				bool enabled;
				using(var uow = _db.UnitOfWork) {
					var config = uow.GuildConfigs.For(channel.Guild.Id);
					enabled = config.FilterInvites = !config.FilterInvites;
					await uow.CompleteAsync().ConfigureAwait(false);
				}

				if(enabled) {
					_service.InviteFilteringServers.Add(channel.Guild.Id);
					await ReplyConfirmLocalized("invite_filter_server_on").ConfigureAwait(false);
				} else {
					_service.InviteFilteringServers.TryRemove(channel.Guild.Id);
					await ReplyConfirmLocalized("invite_filter_server_off").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task ChnlFilterInv() {
				var channel = (ITextChannel) Context.Channel;

				int removed;
				using(var uow = _db.UnitOfWork) {
					var config = uow.GuildConfigs.For(channel.Guild.Id,
						set => set.Include(gc => gc.FilterInvitesChannelIds));
					removed = config.FilterInvitesChannelIds.RemoveWhere(fc => fc.ChannelId == channel.Id);
					if(removed == 0) {
						config.FilterInvitesChannelIds.Add(new FilterChannelId {
							ChannelId = channel.Id
						});
					}

					await uow.CompleteAsync().ConfigureAwait(false);
				}

				if(removed == 0) {
					_service.InviteFilteringChannels.Add(channel.Id);
					await ReplyConfirmLocalized("invite_filter_channel_on").ConfigureAwait(false);
				} else {
					await ReplyConfirmLocalized("invite_filter_channel_off").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task SrvrFilterWords() {
				var channel = (ITextChannel) Context.Channel;

				bool enabled;
				using(var uow = _db.UnitOfWork) {
					var config = uow.GuildConfigs.For(channel.Guild.Id);
					enabled = config.FilterWords = !config.FilterWords;
					await uow.CompleteAsync().ConfigureAwait(false);
				}

				if(enabled) {
					_service.WordFilteringServers.Add(channel.Guild.Id);
					await ReplyConfirmLocalized("word_filter_server_on").ConfigureAwait(false);
				} else {
					_service.WordFilteringServers.TryRemove(channel.Guild.Id);
					await ReplyConfirmLocalized("word_filter_server_off").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task ChnlFilterWords() {
				var channel = (ITextChannel) Context.Channel;

				int removed;
				using(var uow = _db.UnitOfWork) {
					var config = uow.GuildConfigs.For(channel.Guild.Id,
						set => set.Include(gc => gc.FilterWordsChannelIds));
					removed = config.FilterWordsChannelIds.RemoveWhere(fc => fc.ChannelId == channel.Id);
					if(removed == 0) {
						config.FilterWordsChannelIds.Add(new FilterChannelId {
							ChannelId = channel.Id
						});
					}

					await uow.CompleteAsync().ConfigureAwait(false);
				}

				if(removed == 0) {
					_service.WordFilteringChannels.Add(channel.Id);
					await ReplyConfirmLocalized("word_filter_channel_on").ConfigureAwait(false);
				} else {
					_service.WordFilteringChannels.TryRemove(channel.Id);
					await ReplyConfirmLocalized("word_filter_channel_off").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWord([Remainder] string word) {
				var channel = (ITextChannel) Context.Channel;

				word = word?.Trim().ToLowerInvariant();

				if(string.IsNullOrWhiteSpace(word))
					return;

				int removed;
				using(var uow = _db.UnitOfWork) {
					var config = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.FilteredWords));

					removed = config.FilteredWords.RemoveWhere(fw => fw.Word.Trim().ToLowerInvariant() == word);

					if(removed == 0)
						config.FilteredWords.Add(new FilteredWord {
							Word = word
						});

					await uow.CompleteAsync().ConfigureAwait(false);
				}

				var filteredWords =
					_service.ServerFilteredWords.GetOrAdd(channel.Guild.Id, new ConcurrentHashSet<string>());

				if(removed == 0) {
					filteredWords.Add(word);
					await ReplyConfirmLocalized("filter_word_add", Format.Code(word)).ConfigureAwait(false);
				} else {
					filteredWords.TryRemove(word);
					await ReplyConfirmLocalized("filter_word_remove", Format.Code(word)).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task LstFilterWords(int page = 1) {
				page--;
				if(page < 0)
					return;

				var channel = (ITextChannel) Context.Channel;

				_service.ServerFilteredWords.TryGetValue(channel.Guild.Id, out var fwHash);
				if(fwHash is null)
					return;
				var fws = fwHash.ToArray();

				await channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient,
						page,
						curPage => new EmbedBuilder()
							.WithTitle(GetText("filter_word_list"))
							.WithDescription(string.Join("\n", fws.Skip(curPage * 10).Take(10))), fws.Length / 10, hasPerms: gp => true)
					.ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task SrvrFilterZalgo() {
				var channel = (ITextChannel) Context.Channel;

				bool enabled;
				using(var uow = _db.UnitOfWork) {
					var gc = uow.GuildConfigs.For(channel.Guild.Id);
					enabled = gc.FilterZalgo = !gc.FilterZalgo;
					await uow.CompleteAsync().ConfigureAwait(false);
				}

				if(enabled) {
					_service.ZalgoFilteringServers.Add(channel.Guild.Id);
					await ReplyConfirmLocalized("zalgo_filter_server_on").ConfigureAwait(false);
				} else {
					_service.ZalgoFilteringServers.TryRemove(channel.Guild.Id);
					await ReplyConfirmLocalized("zalgo_filter_server_off").ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task ChnlFilterZalgo() {
				var channel = (ITextChannel) Context.Channel;

				int removed;
				using(var uow = _db.UnitOfWork) {
					var gc = uow.GuildConfigs.For(channel.Guild.Id,
						set => set.Include(tgc => tgc.FilterZalgoChannelIds));
					removed = gc.FilterZalgoChannelIds.RemoveWhere(zfc => zfc.ChannelId == channel.Id);
					if(removed == 0) {
						gc.FilterZalgoChannelIds.Add(new ZalgoFilterChannel {
							ChannelId = channel.Id
						});
					}

					await uow.CompleteAsync().ConfigureAwait(false);
				}

				if(removed == 0) {
					_service.ZalgoFilteringChannels.Add(channel.Id);
					await ReplyConfirmLocalized("zalgo_filter_channel_on").ConfigureAwait(false);
				} else {
					_service.ZalgoFilteringChannels.TryRemove(channel.Id);
					await ReplyConfirmLocalized("zalgo_filter_channel_off").ConfigureAwait(false);
				}
			}
		}
	}
}