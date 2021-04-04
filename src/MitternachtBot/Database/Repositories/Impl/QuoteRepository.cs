using System;
using System.Collections.Generic;
using System.Linq;
using Mitternacht.Database.Models;
using MoreLinq;

namespace Mitternacht.Database.Repositories.Impl {
	public class QuoteRepository : Repository<Quote>, IQuoteRepository {
		public QuoteRepository(MitternachtContext context) : base(context) { }

		public IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword)
			=> _set.AsQueryable().Where(q => q.GuildId == guildId).AsEnumerable().Where(q => q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));

		public IQueryable<Quote> GetAllForGuild(ulong guildId)
			=> _set.AsQueryable().Where(q => q.GuildId == guildId);

		public Quote GetRandomQuoteByKeyword(ulong guildId, string keyword)
			=> _set.AsQueryable().Where(q => q.GuildId == guildId).AsEnumerable().Where(q => q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)).Shuffle().FirstOrDefault();

		public Quote SearchQuoteKeywordText(ulong guildId, string keyword, string text)
			=> _set.AsQueryable().Where(q => q.GuildId == guildId).AsEnumerable().Where(q => q.Text.Contains(text, StringComparison.OrdinalIgnoreCase) && q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)).Shuffle().FirstOrDefault();

		public void RemoveAllByKeyword(ulong guildId, string keyword)
			=> _set.RemoveRange(_set.AsQueryable().Where(q => q.GuildId == guildId).AsEnumerable().Where(q => q.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase)));
	}
}
