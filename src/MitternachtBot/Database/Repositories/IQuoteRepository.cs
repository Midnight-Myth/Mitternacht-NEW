using System.Collections.Generic;
using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IQuoteRepository : IRepository<Quote> {
		IEnumerable<Quote> GetAllQuotesByKeyword(ulong guildId, string keyword);
		Quote GetRandomQuoteByKeyword(ulong guildId, string keyword);
		Quote SearchQuoteKeywordText(ulong guildId, string keyword, string text);
		IQueryable<Quote> GetAllForGuild(ulong guildId);
		void RemoveAllByKeyword(ulong guildId, string keyword);
	}
}
