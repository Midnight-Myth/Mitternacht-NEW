using System.Linq;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IReminderRepository : IRepository<Reminder> {
		IQueryable<Reminder> GetRemindersForGuilds(ulong[] guildIds);
	}
}
