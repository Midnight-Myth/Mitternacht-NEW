using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Common.Replacements;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Modules.Utility {
	public partial class Utility {
		[Group]
		public class QuoteCommands : MitternachtSubmodule {
			private readonly IUnitOfWork uow;

			public QuoteCommands(IUnitOfWork uow) {
				this.uow = uow;
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task ListQuotes(int page = 1)
				=> await ListQuotes(null, page).ConfigureAwait(false);

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task ListQuotes(IGuildUser user = null, int page = 1) {
				if(page < 1)
					page = 1;

				var isUserNull = user == null;
				const int elementsPerPage = 16;

				var qs = uow.Quotes.GetAllForGuild(Context.Guild.Id);
				if(!isUserNull) {
					var uid = user.Id;
					qs = qs.Where(q => q.AuthorId == uid);
				}
				var quotes = qs.OrderBy(q => q.Id).ToImmutableList();

				if(!quotes.Any()) {
					await ReplyErrorLocalized("quotes_page_none").ConfigureAwait(false);
					return;
				}

				var pageCount = (int)Math.Ceiling(quotes.Count * 1d / elementsPerPage);
				page = page > pageCount ? pageCount : page;

				var title = isUserNull
					? GetText("quotes_page", "{page}")
					: GetText("quotes_user_page", "{page}", user.ToString());

				await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, currentPage =>
						new EmbedBuilder()
							.WithOkColor()
							.WithTitle(title.Replace("{page}", $"{currentPage + 1}"))
							.WithDescription(string.Join("\n",
								quotes.Skip(currentPage * elementsPerPage).Take(elementsPerPage).Select(q => isUserNull
									? GetText("quotes_list_item_author", q.Id, Format.Bold(q.Keyword.SanitizeMentions()), q.AuthorName.SanitizeMentions())
									: GetText("quotes_list_item", q.Id, Format.Bold(q.Keyword.SanitizeMentions()))))),
					pageCount, true, new[] { Context.User as IGuildUser });
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task ShowQuote([Remainder] string keyword) {
				if(string.IsNullOrWhiteSpace(keyword))
					return;

				var quote = uow.Quotes.GetRandomQuoteByKeyword(Context.Guild.Id, keyword);

				if(quote != null) {
					var replacer = new ReplacementBuilder().WithDefault(Context).Build();

					if(CREmbed.TryParse(quote.Text, out var crembed)) {
						replacer.Replace(crembed);
						await Context.Channel.EmbedAsync(crembed.ToEmbedBuilder(), crembed.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
					} else {
						await Context.Channel.SendMessageAsync($"`#{quote.Id}` 📣 {quote.Keyword.SanitizeMentions()}: {replacer.Replace(quote.Text)?.SanitizeMentions()}").ConfigureAwait(false);
					}
				} else {
					await ReplyErrorLocalized("quote_not_found", keyword).ConfigureAwait(false);
					return;
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task QuoteSearch(string keyword, [Remainder] string text) {
				if(string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
					return;

				var keywordquote= uow.Quotes.SearchQuoteKeywordText(Context.Guild.Id, keyword, text);

				if(keywordquote != null) {
					await Context.Channel.SendMessageAsync($"`#{keywordquote.Id}` 💬 {keywordquote.Keyword}:  {keywordquote.Text.SanitizeMentions()}").ConfigureAwait(false);
				} else {
					await ReplyErrorLocalized("quote_text_not_found", keyword).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task QuoteId(ushort id) {
				var quote = uow.Quotes.Get(id);

				var replacer = new ReplacementBuilder().WithDefault(Context).Build();

				if(quote == null) {
					await Context.Channel.SendErrorAsync(GetText("quotes_notfound", id));
				} else if(CREmbed.TryParse(quote.Text, out var crembed)) {
					replacer.Replace(crembed);
					await Context.Channel.EmbedAsync(crembed.ToEmbedBuilder(), crembed.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
				} else
					await Context.Channel.SendMessageAsync($"`#{quote.Id}` 🗯️ {quote.Keyword.ToLowerInvariant().SanitizeMentions()}:  {replacer.Replace(quote.Text)?.SanitizeMentions()}").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task AddQuote(string keyword, [Remainder] string text) {
				if(string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
					return;

				uow.Quotes.Add(new Quote {
					AuthorId = Context.User.Id,
					AuthorName = Context.User.Username,
					GuildId = Context.Guild.Id,
					Keyword = keyword.SanitizeMentions(),
					Text = text
				});
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized("quote_added").ConfigureAwait(false);
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			public async Task QuoteDelete(ushort id) {
				var isAdmin = ((IGuildUser) Context.User).GuildPermissions.Administrator;

				var q = uow.Quotes.Get(id);

				if(q == null || !isAdmin && q.AuthorId != Context.User.Id) {
					await ErrorLocalized("quotes_remove_none").ConfigureAwait(false);
				} else {
					uow.Quotes.Remove(q);
					await uow.SaveChangesAsync(false).ConfigureAwait(false);
					await ConfirmLocalized("quote_deleted", id).ConfigureAwait(false);
				}
			}

			[MitternachtCommand, Usage, Description, Aliases]
			[RequireContext(ContextType.Guild)]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task DelAllQuotes([Remainder] string keyword) {
				if(string.IsNullOrWhiteSpace(keyword))
					return;

				uow.Quotes.RemoveAllByKeyword(Context.Guild.Id, keyword);
				await uow.SaveChangesAsync(false).ConfigureAwait(false);

				await ReplyConfirmLocalized("quotes_deleted", Format.Bold(keyword.SanitizeMentions())).ConfigureAwait(false);
			}
		}
	}
}
