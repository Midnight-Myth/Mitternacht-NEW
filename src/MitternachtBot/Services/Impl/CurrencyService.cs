using System;
using System.Threading.Tasks;
using Discord;
using Mitternacht.Database;
using Mitternacht.Database.Models;

namespace Mitternacht.Services.Impl {
	public class CurrencyService : IMService {
		private readonly DbService _db;

		public CurrencyService(DbService db) {
			_db = db;
		}

		public Task<bool> RemoveAsync(IGuildUser author, string reason, long amount)
			=> RemoveAsync(author.GuildId, author.Id, reason, amount);

		public async Task<bool> RemoveAsync(ulong guildId, ulong authorId, string reason, long amount, IUnitOfWork uow = null) {
			if(amount < 0)
				throw new ArgumentException(null, nameof(amount));

			if(uow == null) {
				using var _uow = _db.UnitOfWork;
				var toReturn = InternalRemoveCurrency(guildId, authorId, reason, amount, _uow);
				await _uow.SaveChangesAsync().ConfigureAwait(false);

				return toReturn;
			} else {
				return InternalRemoveCurrency(guildId, authorId, reason, amount, uow);
			}
		}

		private bool InternalRemoveCurrency(ulong guildId, ulong authorId, string reason, long amount, IUnitOfWork uow) {
			var success = uow.Currency.TryAddCurrencyValue(guildId, authorId, -amount);

			if(success) {
				uow.CurrencyTransactions.Add(new CurrencyTransaction {
					GuildId = guildId,
					UserId  = authorId,
					Reason  = reason,
					Amount  = -amount,
				});

				return true;
			} else {
				return false;
			}
		}

		public async Task AddToManyAsync(ulong guildId, string reason, long amount, params ulong[] userIds) {
			using var uow = _db.UnitOfWork;

			foreach(var userId in userIds) {
				var transaction = new CurrencyTransaction {
					GuildId = guildId,
					UserId  = userId,
					Reason  = reason,
					Amount  = amount,
				};

				uow.Currency.TryAddCurrencyValue(guildId, userId, amount);
				uow.CurrencyTransactions.Add(transaction);
			}

			await uow.SaveChangesAsync().ConfigureAwait(false);
		}

		public Task AddAsync(IGuildUser author, string reason, long amount, IUnitOfWork uow = null)
			=> AddAsync(author.GuildId, author.Id, reason, amount, uow);

		public async Task AddAsync(ulong guildId, ulong receiverId, string reason, long amount, IUnitOfWork uow = null) {
			if(amount < 0)
				throw new ArgumentException(null, nameof(amount));

			var transaction = new CurrencyTransaction {
				GuildId = guildId,
				UserId  = receiverId,
				Reason  = reason,
				Amount  = amount,
			};

			if(uow == null) {
				using var _uow = _db.UnitOfWork;
				_uow.Currency.TryAddCurrencyValue(guildId, receiverId, amount);
				_uow.CurrencyTransactions.Add(transaction);
				await _uow.SaveChangesAsync();
			} else {
				uow.Currency.TryAddCurrencyValue(guildId, receiverId, amount);
				uow.CurrencyTransactions.Add(transaction);
			}
		}
	}
}