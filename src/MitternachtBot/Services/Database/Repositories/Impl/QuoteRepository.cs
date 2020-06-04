using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Extensions;
using Mitternacht.Services.Database.Models;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class QuoteRepository : Repository<Quote>, IQuoteRepository {
		public QuoteRepository(DbContext context) : base(context) { }

		public IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword)
			=> _set.Where((Expression<Func<Quote, bool>>)(q => q.GuildId == guildId && q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)));

		public IEnumerable<Quote> GetAllForGuild(ulong guildId)
			=> _set.Where((Expression<Func<Quote, bool>>)(q => q.GuildId == guildId)).ToList();

		public Quote GetRandomQuoteByKeyword(ulong guildId, string keyword)
			=> _set.Where((Expression<Func<Quote, bool>>)(q => q.GuildId == guildId && q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))).AsEnumerable().Shuffle().FirstOrDefault();

		public Quote SearchQuoteKeywordText(ulong guildId, string keyword, string text)
			=> _set.Where((Expression<Func<Quote, bool>>)(q => q.Text.ContainsNoCase(text, StringComparison.OrdinalIgnoreCase) && q.GuildId == guildId && q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))).AsEnumerable().Shuffle().FirstOrDefault();

		public void RemoveAllByKeyword(ulong guildId, string keyword)
			=> _set.RemoveRange(_set.Where((Expression<Func<Quote, bool>>)(x => x.GuildId == guildId && x.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))));
	}
}
