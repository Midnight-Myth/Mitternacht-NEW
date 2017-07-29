using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class DailyMoneyCommands : NadekoSubmodule
        {
            private readonly IBotConfigProvider _bc;
            private readonly DbService _db;
            private readonly CurrencyService _currency;

            private string CurrencyName => _bc.BotConfig.CurrencyName;
            private string CurrencyPluralName => _bc.BotConfig.CurrencyPluralName;
            private string CurrencySign => _bc.BotConfig.CurrencySign;

            public DailyMoneyCommands(IBotConfigProvider bc, DbService db, CurrencyService currency)
            {
                _bc = bc;
                _db = db;
                _currency = currency;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task DailyMoney()
            {
                var user = (IGuildUser)Context.User;

                bool canReceiveDailyMoney;
                using (var uow = _db.UnitOfWork)
                {
                    canReceiveDailyMoney = uow.DailyMoney.TryUpdateState(user.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                if (canReceiveDailyMoney)
                {
                    IEnumerable<IRole> userRoles = user.GetRoles().OrderBy(r => -r.Position);
                    _log.Info($"userrolescount: {userRoles.Count()}");
                    IEnumerable<RoleMoney> roleMoneys;
                    using (var uow = _db.UnitOfWork)
                    {
                        roleMoneys = uow.RoleMoney.GetAll().OrderBy(m => m.Priority);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    _log.Info($"rolemoneyscount: {roleMoneys.Count()}");
                    userRoles = userRoles.Where(r => roleMoneys.FirstOrDefault(m => m.RoleId == r.Id) != null).OrderBy(r => -r.Position);
                    _log.Info($"userrolescount: {userRoles.Count()}");
                    roleMoneys = roleMoneys.Where(m => userRoles.FirstOrDefault(r => r.Id == m.RoleId) != null);
                    _log.Info($"rolemoneyscount: {roleMoneys.Count()}");
                    if (roleMoneys.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync($"Deine Rollen erlauben kein DailyMoney, also bekommst du nichts, {Context.User.Mention}!");
                        using (var uow = _db.UnitOfWork)
                        {
                            uow.DailyMoney.TryResetReceived(user.Id);
                            await uow.CompleteAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        var rm = roleMoneys.Where(m => m.Priority == roleMoneys.First().Priority).OrderBy(m => -userRoles.First(r => r.Id == m.RoleId).Position).First();
                        var role = userRoles.First(r => r.Id == rm.RoleId);
                        await _currency.AddAsync(user, $"Daily Reward ({role.Name})", rm.Money, false).ConfigureAwait(false);
                        await Context.Channel.SendMessageAsync($"{Context.User.Mention} hat sich seinen täglichen \"{role.Name}\"-Anteil von {rm.Money} {CurrencySign} abgeholt.");
                    }
                }
                else await Context.Channel.SendMessageAsync($"Du hast deinen täglichen Anteil heute bereits abgeholt, {Context.User.Mention}");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task SetRoleMoney(IRole role, long money, int priority)
            {
                using (var uow = _db.UnitOfWork)
                {
                    uow.RoleMoney.SetMoney(role.Id, money);
                    uow.RoleMoney.SetPriority(role.Id, priority);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await Context.Channel.SendMessageAsync($"Rolle {role.Name} bekommt nun {money} {CurrencySign} mit Priorität {priority}.");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task SetRoleMoney(IRole role, long money) => await SetRoleMoney(role, money, 0);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task ResetDailyMoney([Remainder]IUser user = null)
            {
                user = user ?? Context.User;
                bool wasReset;
                using (var uow = _db.UnitOfWork)
                {
                    wasReset = uow.DailyMoney.TryResetReceived(user.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await Context.Channel.SendMessageAsync(wasReset ? $"{user.Username} kann seinen täglichen Anteil nochmal abholen." : $"{user.Username} hat seinen täglichen Anteil noch nicht abgeholt.");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task RemoveRoleMoney(IRole role)
            {
                bool removed;
                using (var uow = _db.UnitOfWork)
                {
                    removed = uow.RoleMoney.Remove(role.Id);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await Context.Channel.SendMessageAsync(removed ? $"Rolle \"{role.Name}\" wurde von der Gehaltsliste entfernt." : $"Rolle \"{role.Name}\" steht nicht auf der Gehaltsliste!");
            }
        }
    }
}
