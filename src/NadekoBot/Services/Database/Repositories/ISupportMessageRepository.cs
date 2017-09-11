using System;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services.Database.Repositories
{
    public interface ISupportMessageRepository : IRepository<SupportMessage>
    {
        SupportMessage Create(ulong guildId, ulong userId, ulong editorId, ulong channelId, DateTime createdAt, string message);
        bool Delete(int id);
    }
}