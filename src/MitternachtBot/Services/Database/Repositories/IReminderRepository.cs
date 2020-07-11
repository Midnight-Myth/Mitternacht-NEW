using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories {
	public interface IReminderRepository : IRepository<Reminder> {
		IEnumerable<Reminder> GetIncludedReminders(IEnumerable<ulong> guildIds);
	}
}
