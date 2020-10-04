using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Permissions.Services;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions {
		[Group]
		public class FilterCommands : MitternachtSubmodule<FilterService> {
			private readonly IUnitOfWork uow;

			public FilterCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterInvitesServer() {
				var gc        = uow.GuildConfigs.For(Context.Guild.Id);
				var enabled   = gc.FilterInvites = !gc.FilterInvites;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(enabled ? "invite_filter_server_on" : "invite_filter_server_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterInvitesChannel() {
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilterInvitesChannelIds));
				var removed   = gc.FilterInvitesChannelIds.RemoveWhere(fc => fc.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					gc.FilterInvitesChannelIds.Add(new FilterChannelId {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "invite_filter_channel_on" : "invite_filter_channel_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWordsServer() {
				var gc        = uow.GuildConfigs.For(Context.Guild.Id);
				var enabled   = gc.FilterWords = !gc.FilterWords;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(enabled ? "word_filter_server_on" : "word_filter_server_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWordsChannel() {
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilterWordsChannelIds));
				var removed   = gc.FilterWordsChannelIds.RemoveWhere(fc => fc.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					gc.FilterWordsChannelIds.Add(new FilterChannelId {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "word_filter_channel_on" : "word_filter_channel_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWord([Remainder] string word) {
				if(string.IsNullOrWhiteSpace(word))
					return;

				word = word.Trim();
				
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilteredWords));
				int removed   = gc.FilteredWords.RemoveWhere(fw => fw.Word.Equals(word, System.StringComparison.OrdinalIgnoreCase));
				if(removed == 0) {
					gc.FilteredWords.Add(new FilteredWord {
						Word = word
					});
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "filter_word_add" : "filter_word_remove", Format.Code(word)).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilteredWords(int page = 1) {
				page--;
				if(page < 0)
					return;

				const int elementsPerPage = 10;

				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilteredWords));
				var filteredWords = gc.FilteredWords.Select(fw => fw.Word).ToArray();

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => new EmbedBuilder().WithOkColor().WithTitle(GetText("filter_word_list")).WithDescription(string.Join("\n", filteredWords.Skip(currentPage * elementsPerPage).Take(elementsPerPage))), (int)Math.Ceiling(filteredWords.Length * 1d / elementsPerPage), reactUsers: new[] { Context.User as IGuildUser }).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterZalgoServer() {
				var gc        = uow.GuildConfigs.For(Context.Guild.Id);
				var enabled   = gc.FilterZalgo = !gc.FilterZalgo;
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(enabled ? "zalgo_filter_server_on" : "zalgo_filter_server_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterZalgoChannel() {
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(fzc => fzc.FilterZalgoChannelIds));
				var removed   = gc.FilterZalgoChannelIds.RemoveWhere(zfc => zfc.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					gc.FilterZalgoChannelIds.Add(new ZalgoFilterChannel {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "zalgo_filter_channel_on" : "zalgo_filter_channel_off").ConfigureAwait(false);
			}
		}
	}
}