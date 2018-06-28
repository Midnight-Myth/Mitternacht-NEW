using System;
using System.Collections.Generic;
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
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class QuoteCommands : MitternachtSubmodule
        {
            private readonly DbService _db;

            public QuoteCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListQuotes(int page = 1)
                => await ListQuotes(null, page).ConfigureAwait(false);

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ListQuotes(IGuildUser user = null, int page = 1)
            {
                if (page < 1) page = 1;

                var isUserNull = user == null;
                const int elementsPerPage = 16;

                List<Quote> quotes;
                using (var uow = _db.UnitOfWork)
                {
                    var qs = uow.Quotes.GetAllForGuild(Context.Guild.Id);
                    if (!isUserNull)
                    {
                        var uid = user.Id;
                        qs = qs.Where(q => q.AuthorId == uid);
                    }
                    quotes = qs.OrderBy(q => q.Id).ToList();
                }

                if (!quotes.Any())
                {
                    await ReplyErrorLocalized("quotes_page_none").ConfigureAwait(false);
                    return;
                }

                var pagecount = (int)Math.Ceiling(quotes.Count * 1.0 / elementsPerPage);
                page = page > pagecount ? pagecount : page;

                var title = isUserNull
                    ? GetText("quotes_page", "{page}")
                    : GetText("quotes_user_page", "{page}", user.ToString());

                await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p =>
                        new EmbedBuilder()
                            .WithTitle(title.Replace("{page}", $"{p+1}"))
                            .WithDescription(string.Join("\n",
                                quotes.Skip(p * elementsPerPage).Take(elementsPerPage).Select(q => isUserNull
                                    ? GetText("quotes_list_item", q.Id, Format.Bold(q.Keyword.SanitizeMentions()))
                                    : GetText("quotes_list_item_author", q.Id, Format.Bold(q.Keyword.SanitizeMentions()), q.AuthorName.SanitizeMentions())))),
                    pagecount - 1, true, new[] { Context.User as IGuildUser });
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShowQuote([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword)) return;

                Quote quote;
                using (var uow = _db.UnitOfWork)
                {
                    quote = uow.Quotes.GetRandomQuoteByKeyword(Context.Guild.Id, keyword);
                }

                if (quote == null)
                {
                    await ReplyErrorLocalized("quote_not_found", keyword).ConfigureAwait(false);
                    return;
                }

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await Context.Channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText?.SanitizeMentions() ?? "").ConfigureAwait(false);
                    return;
                }
                await Context.Channel.SendMessageAsync($"`#{quote.Id}` 📣 {quote.Keyword.SanitizeMentions()}: {rep.Replace(quote.Text)?.SanitizeMentions()}").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearch(string keyword, [Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                Quote keywordquote;
                using (var uow = _db.UnitOfWork)
                {
                    keywordquote = uow.Quotes.SearchQuoteKeywordText(Context.Guild.Id, keyword, text);
                }

                if (keywordquote == null)
                {
                    await ReplyErrorLocalized("quote_text_not_found", keyword).ConfigureAwait(false);
                    return;
                }

                await Context.Channel.SendMessageAsync($"`#{keywordquote.Id}` 💬 {keywordquote.Keyword}:  {keywordquote.Text.SanitizeMentions()}").ConfigureAwait(false);
            }
            
            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteId(ushort id)
            {
                using (var uow = _db.UnitOfWork)
                { 
                    var qfromid = uow.Quotes.Get(id);

                    var rep = new ReplacementBuilder()
                        .WithDefault(Context)
                        .Build();

                    if (qfromid == null)
                    {
                        await Context.Channel.SendErrorAsync(GetText("quotes_notfound", id));
                    }
                    else if (CREmbed.TryParse(qfromid.Text, out var crembed))
                    {
                        rep.Replace(crembed);
                        await Context.Channel.EmbedAsync(crembed.ToEmbed(), crembed.PlainText?.SanitizeMentions() ?? "")
                            .ConfigureAwait(false);
                    }
                    else
                        await Context.Channel
                            .SendMessageAsync(
                                $"`#{qfromid.Id}` 🗯️ {qfromid.Keyword.ToLowerInvariant().SanitizeMentions()}:  {rep.Replace(qfromid.Text)?.SanitizeMentions()}")
                            .ConfigureAwait(false);
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AddQuote(string keyword, [Remainder] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;
                
                using (var uow = _db.UnitOfWork)
                {
                    uow.Quotes.Add(new Quote
                    {
                        AuthorId = Context.User.Id,
                        AuthorName = Context.User.Username,
                        GuildId = Context.Guild.Id,
                        Keyword = keyword.SanitizeMentions(),
                        Text = text
                    });
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await ReplyConfirmLocalized("quote_added").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteDelete(ushort id)
            {
                var isAdmin = ((IGuildUser) Context.User).GuildPermissions.Administrator;
                
                using (var uow = _db.UnitOfWork)
                {
                    var q = uow.Quotes.Get(id);

                    if (q == null || !isAdmin && q.AuthorId != Context.User.Id)
                    {
                        await ErrorLocalized("quotes_remove_none").ConfigureAwait(false);
                    }
                    else
                    {
                        uow.Quotes.Remove(q);
                        await uow.CompleteAsync().ConfigureAwait(false);
                        await ConfirmLocalized("quote_deleted", id).ConfigureAwait(false);
                    }
                }
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task DelAllQuotes([Remainder] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword)) return;

                using (var uow = _db.UnitOfWork)
                {
                    uow.Quotes.RemoveAllByKeyword(Context.Guild.Id, keyword);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await ReplyConfirmLocalized("quotes_deleted", Format.Bold(keyword.SanitizeMentions())).ConfigureAwait(false);
            }
        }
    }
}
