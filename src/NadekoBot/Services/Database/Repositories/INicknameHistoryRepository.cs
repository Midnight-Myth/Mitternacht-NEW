using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface INicknameHistoryRepository : IRepository<NicknameHistoryModel>
    {
        /// <summary>
        /// Returns an ordered list of all nicknames a user had in a guild.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        IEnumerable<NicknameHistoryModel> GetGuildUserNames(ulong guildId, ulong userId);
        /// <summary>
        /// Returns a non-ordered list of all nicknames a user had.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        IEnumerable<NicknameHistoryModel> GetUserNames(ulong userId);

        /// <summary>
        /// Closes the current nickname entry (if possible) and creates a new entry.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId"></param>
        /// <param name="nickname"></param>
        /// <param name="discriminator"></param>
        /// <returns></returns>
        bool AddUsername(ulong guildId, ulong userId, string nickname, ushort discriminator);
        
        /// <summary>
        /// Closes the current nickname entry if possible.
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="userId"></param>
        /// <returns>True when closed, otherwise false</returns>
        bool CloseNickname(ulong guildId, ulong userId);
    }
}