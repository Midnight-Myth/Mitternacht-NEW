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
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Permissions {
	public partial class Permissions {
		[Group]
		public class FilterCommands : MitternachtSubmodule<FilterService> {
			private readonly DbService _db;

			public FilterCommands(DbService db) {
				_db = db;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterInvitesServer() {
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id);
				var enabled   = gc.FilterInvites = !gc.FilterInvites;
				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(enabled ? "invite_filter_server_on" : "invite_filter_server_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterInvitesChannel() {
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilterInvitesChannelIds));
				var removed   = gc.FilterInvitesChannelIds.RemoveWhere(fc => fc.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					gc.FilterInvitesChannelIds.Add(new FilterChannelId {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "invite_filter_channel_on" : "invite_filter_channel_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWordsServer() {
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id);
				var enabled   = gc.FilterWords = !gc.FilterWords;
				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(enabled ? "word_filter_server_on" : "word_filter_server_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWordsChannel() {
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilterWordsChannelIds));
				var removed   = gc.FilterWordsChannelIds.RemoveWhere(fc => fc.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					gc.FilterWordsChannelIds.Add(new FilterChannelId {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "word_filter_channel_on" : "word_filter_channel_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterWord([Remainder] string word) {
				if(string.IsNullOrWhiteSpace(word))
					return;

				word = word.Trim();
				
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilteredWords));
				int removed   = gc.FilteredWords.RemoveWhere(fw => fw.Word.Equals(word, System.StringComparison.OrdinalIgnoreCase));
				if(removed == 0) {
					gc.FilteredWords.Add(new FilteredWord {
						Word = word
					});
				}

				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "filter_word_add" : "filter_word_remove", Format.Code(word)).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilteredWords(int page = 1) {
				page--;
				if(page < 0)
					return;

				const int WordsPerPage = 10;

				using var uow = _db.UnitOfWork;
				var gc = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(gc => gc.FilteredWords));
				var filteredWords = gc.FilteredWords.Select(fw => fw.Word).ToArray();

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page, currentPage => new EmbedBuilder().WithOkColor().WithTitle(GetText("filter_word_list")).WithDescription(string.Join("\n", filteredWords.Skip(currentPage * WordsPerPage).Take(WordsPerPage))), filteredWords.Length / WordsPerPage, hasPerms: gp => true).ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterZalgoServer() {
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id);
				var enabled   = gc.FilterZalgo = !gc.FilterZalgo;
				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(enabled ? "zalgo_filter_server_on" : "zalgo_filter_server_off").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task FilterZalgoChannel() {
				using var uow = _db.UnitOfWork;
				var gc        = uow.GuildConfigs.For(Context.Guild.Id, set => set.Include(fzc => fzc.FilterZalgoChannelIds));
				var removed   = gc.FilterZalgoChannelIds.RemoveWhere(zfc => zfc.ChannelId == Context.Channel.Id);
				if(removed == 0) {
					gc.FilterZalgoChannelIds.Add(new ZalgoFilterChannel {
						ChannelId = Context.Channel.Id
					});
				}

				await uow.CompleteAsync().ConfigureAwait(false);

				await ReplyConfirmLocalized(removed == 0 ? "zalgo_filter_channel_on" : "zalgo_filter_channel_off").ConfigureAwait(false);
			}
		}
	}
}