using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class CustomReactionsRepository : Repository<CustomReaction>, ICustomReactionRepository {
		public CustomReactionsRepository(DbContext context) : base(context) {
		}
	}
}
