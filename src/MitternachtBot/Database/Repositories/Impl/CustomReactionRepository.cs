using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories.Impl {
	public class CustomReactionsRepository : Repository<CustomReaction>, ICustomReactionRepository {
		public CustomReactionsRepository(MitternachtContext context) : base(context) {
		}
	}
}
