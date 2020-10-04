using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System;

namespace Mitternacht.Services.Database.Repositories.Impl {
	public class ReminderRepository : Repository<Reminder>, IReminderRepository {
		public ReminderRepository(DbContext context) : base(context) { }

		public IQueryable<Reminder> GetRemindersForGuilds(ulong[] guildIds)
			=> _set.AsQueryable().Where(x => guildIds.Contains(x.ServerId));
	}
}
