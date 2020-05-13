using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class SupportMessageRepository : Repository<SupportMessage>, ISupportMessageRepository
    {
        public SupportMessageRepository(DbContext context) : base(context) { }

        public SupportMessage Create(ulong guildId, ulong userId, ulong editorId, ulong channelId, DateTime createdAt, string message) {
            SupportMessage msg;
            _set.Add(msg = new SupportMessage {
                GuildId = guildId,
                UserId = userId,
                EditorId = editorId,
                ChannelId = channelId,
                CreatedAt = createdAt,
                Message = message
            });
            return msg;
        }

        public bool Delete(int id) {
            var msg = _set.FirstOrDefault(m => m.Id == id);
            if (msg == null) return false;
            _set.Remove(msg);
            return true;
        }
    }
}