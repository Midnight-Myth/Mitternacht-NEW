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

        public async Task<bool> RemoveAsync(IUser author, string reason, long amount, bool sendMessage)
        {
            var success = await RemoveAsync(author.Id, reason, amount);

            if (!success || !sendMessage) return success;
            try { await author.SendErrorAsync($"`You lost:` {amount} {_config.BotConfig.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }

            return true;
        }

        public async Task<bool> RemoveAsync(ulong authorId, string reason, long amount, IUnitOfWork uow = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));

            if (uow != null) return InternalRemoveCurrency(authorId, reason, amount, uow);
            using (uow = _db.UnitOfWork)
            {
                var toReturn = InternalRemoveCurrency(authorId, reason, amount, uow);
                await uow.SaveChangesAsync().ConfigureAwait(false);
                return toReturn;
            }
        }

        private bool InternalRemoveCurrency(ulong authorId, string reason, long amount, IUnitOfWork uow)
        {
            var success = uow.Currency.TryUpdateState(authorId, -amount);
            if (!success)
                return false;
            uow.CurrencyTransactions.Add(new CurrencyTransaction()
            {
                UserId = authorId,
                Reason = reason,
                Amount = -amount,
            });
            return true;
        }

        public async Task AddToManyAsync(string reason, long amount, params ulong[] userIds)
        {
            using (var uow = _db.UnitOfWork)
            {
                foreach (var userId in userIds)
                {
                    var transaction = new CurrencyTransaction
                    {
                        UserId = userId,
                        Reason = reason,
                        Amount = amount,
                    };
                    uow.Currency.TryUpdateState(userId, amount);
                    uow.CurrencyTransactions.Add(transaction);
                }

                await uow.SaveChangesAsync();
            }
        }

        public async Task AddAsync(IUser author, string reason, long amount, bool sendMessage)
        {
            await AddAsync(author.Id, reason, amount);

            if (sendMessage)
                try { await author.SendConfirmAsync($"`You received:` {amount} {_config.BotConfig.CurrencySign}\n`Reason:` {reason}").ConfigureAwait(false); } catch { }
        }

        public async Task AddAsync(ulong receiverId, string reason, long amount, IUnitOfWork uow = null)
        {
            if (amount < 0)
                throw new ArgumentNullException(nameof(amount));

            var transaction = new CurrencyTransaction
            {
                UserId = receiverId,
                Reason = reason,
                Amount = amount,
            };

            if (uow == null)
                using (uow = _db.UnitOfWork)
                {
                    uow.Currency.TryUpdateState(receiverId, amount);
                    uow.CurrencyTransactions.Add(transaction);
                    await uow.SaveChangesAsync();
                }
            else
            {
                uow.Currency.TryUpdateState(receiverId, amount);
                uow.CurrencyTransactions.Add(transaction);
            }
        }
    }
}