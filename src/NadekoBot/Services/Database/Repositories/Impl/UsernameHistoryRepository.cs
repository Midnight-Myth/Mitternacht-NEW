﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class UsernameHistoryRepository : Repository<UsernameHistoryModel>, IUsernameHistoryRepository
    {
        public UsernameHistoryRepository(DbContext context) : base(context) { }

        public bool AddUsername(ulong userId, string username, ushort discriminator) {
            if (string.IsNullOrWhiteSpace(username)) return false;

            username = username.Trim();
            var current = GetUserNames(userId).FirstOrDefault();
            var now = DateTime.UtcNow;
            if (current != null)
            {
                if (string.Equals(current.Name, username, StringComparison.Ordinal) && current.DiscordDiscriminator == discriminator)
                {
                    if (!current.DateReplaced.HasValue) return false;
                    current.DateReplaced = null;
                    _set.Update(current);
                    return false;
                }

                if (!current.DateReplaced.HasValue)
                {
                    current.DateReplaced = now;
                    _set.Update(current);
                }
            }

            _set.Add(new UsernameHistoryModel {
                UserId = userId,
                Name = username,
                DiscordDiscriminator = discriminator,
                DateSet = now
            });
            return true;
        }

        public IEnumerable<UsernameHistoryModel> GetUserNames(ulong userId)
            => _set.Where(u => u.UserId == userId && !(u is NicknameHistoryModel)).OrderByDescending(u => u.DateSet);
    }
}