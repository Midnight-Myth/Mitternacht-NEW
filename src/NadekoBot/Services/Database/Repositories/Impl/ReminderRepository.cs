using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;
using System;
using System.Linq.Expressions;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class ReminderRepository : Repository<Reminder>, IReminderRepository
    {
        public ReminderRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Reminder> GetIncludedReminders(IEnumerable<ulong> guildIds)
			=> _set.Where((Expression<Func<Reminder, bool>>)(x => guildIds.Contains(x.ServerId))).ToList();
	}
}
