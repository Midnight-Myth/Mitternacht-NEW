using System.Linq;
using Mitternacht.Database.Models;

namespace Mitternacht.Database.Repositories {
	public interface IReminderRepository : IRepository<Reminder> {
		IQueryable<Reminder> GetRemindersForGuilds(ulong[] guildIds);
	}
}
