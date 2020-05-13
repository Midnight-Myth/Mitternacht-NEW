using System;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface ISupportMessageRepository : IRepository<SupportMessage>
    {
        SupportMessage Create(ulong guildId, ulong userId, ulong editorId, ulong channelId, DateTime createdAt, string message);
        bool Delete(int id);
    }
}