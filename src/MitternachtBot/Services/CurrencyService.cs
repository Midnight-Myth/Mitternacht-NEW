using System;
using System.Threading.Tasks;
using Discord;
using Mitternacht.Extensions;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services
{
    public class CurrencyService : IMService
    {
        private readonly IBotConfigProvider _config;
        private readonly DbService _db;

        public CurrencyService(IBotConfigProvider config, DbService db)
        {
            _config = config;
            _db = db;
        }

        public async Task<bool> RemoveAsync(IGuildUser author, string reason, long amount, bool sendMessage)
        {
            var success = await RemoveAsync(author.GuildId, author.Id, reason, amount);

            if (!success || !sendMessage) return success;
            try { await author.SendErrorAsync($"`You lost:` {amount} {_config.BotConfig.CurrencySign} on Server with ID {author.GuildId}.\n`Reason:` {reason}").ConfigureAwait(false); } catch { }

            return true;
        }

        public async Task<bool> RemoveAsync(ulong guildId, ulong authorId, string reason, long amount, IUnitOfWork uow = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));

            if (uow != null) return InternalRemoveCurrency(guildId, authorId, reason, amount, uow);
            using (uow = _db.UnitOfWork)
            {
                var toReturn = InternalRemoveCurrency(guildId, authorId, reason, amount, uow);
                await uow.SaveChangesAsync().ConfigureAwait(false);
                return toReturn;
            }
        }

        private bool InternalRemoveCurrency(ulong guildId, ulong authorId, string reason, long amount, IUnitOfWork uow)
        {
            var success = uow.Currency.TryAddCurrencyValue(guildId, authorId, -amount);
            if (!success)
                return false;
            uow.CurrencyTransactions.Add(new CurrencyTransaction()
            {
				GuildId = guildId,
                UserId = authorId,
                Reason = reason,
                Amount = -amount,
            });
            return true;
        }

        public async Task AddToManyAsync(ulong guildId, string reason, long amount, params ulong[] userIds)
        {
            using (var uow = _db.UnitOfWork)
            {
                foreach (var userId in userIds)
                {
                    var transaction = new CurrencyTransaction
                    {
						GuildId = guildId,
                        UserId = userId,
                        Reason = reason,
                        Amount = amount,
                    };
                    uow.Currency.TryAddCurrencyValue(guildId, userId, amount);
                    uow.CurrencyTransactions.Add(transaction);
                }

                await uow.SaveChangesAsync();
            }
        }

        public async Task AddAsync(IGuildUser author, string reason, long amount, bool sendMessage, IUnitOfWork uow = null)
        {
            await AddAsync(author.GuildId, author.Id, reason, amount, uow);

            if (sendMessage)
                try { await author.SendConfirmAsync($"`You received:` {amount} {_config.BotConfig.CurrencySign} on Server with ID {author.GuildId}.\n`Reason:` {reason}").ConfigureAwait(false); } catch { }
        }

        public async Task AddAsync(ulong guildId, ulong receiverId, string reason, long amount, IUnitOfWork uow = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));

            var transaction = new CurrencyTransaction
            {
				GuildId = guildId,
				UserId = receiverId,
                Reason = reason,
                Amount = amount,
            };

            if (uow == null)
                using (uow = _db.UnitOfWork)
                {
                    uow.Currency.TryAddCurrencyValue(guildId, receiverId, amount);
                    uow.CurrencyTransactions.Add(transaction);
                    await uow.SaveChangesAsync();
                }
            else
            {
                uow.Currency.TryAddCurrencyValue(guildId, receiverId, amount);
                uow.CurrencyTransactions.Add(transaction);
            }
        }
    }
}