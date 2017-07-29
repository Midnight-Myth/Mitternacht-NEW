using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                    IEnumerable<IRole> userRolesAll = user.GetRoles().OrderBy(r => -r.Position);
                    IEnumerable<RoleMoney> roleMoneysAll;
                    using (var uow = _db.UnitOfWork)
                    {
                        roleMoneysAll = uow.RoleMoney.GetAll().OrderBy(m => m.Priority);
                        await uow.CompleteAsync().ConfigureAwait(false);
                    }
                    var userRoles = userRolesAll.Where(r => roleMoneysAll.FirstOrDefault(m => m.RoleId == r.Id) != null).OrderBy(r => -r.Position);
                    var roleMoneys = roleMoneysAll.Where(m => userRolesAll.FirstOrDefault(r => r.Id == m.RoleId) != null);
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

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Payroll(int count, [Remainder]int position = 1)
            {
                var elementsPerList = 20;

                IOrderedEnumerable<RoleMoney> roleMoneys;
                using (var uow = _db.UnitOfWork)
                {
                    roleMoneys = uow.RoleMoney.GetAll().OrderByDescending(rm => (((long)rm.Priority << 32) - Context.Guild.GetRole(rm.RoleId).Position));
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                position--;
                if (position < 0 || position > roleMoneys.Count()) position = 0;
                if (count <= 0 || count > roleMoneys.Count() - position) count = roleMoneys.Count() - position;

                List<string> rankstrings = new List<string>();
                var sb = new StringBuilder();
                sb.AppendLine("__**Gehaltsliste**__");
                for (int i = position; i < count + position; i++)
                {
                    var rm = roleMoneys.ElementAt(i);
                    var role = Context.Guild.GetRole(rm.RoleId);

                    if ((i - position) % elementsPerList == 0) sb.AppendLine($"```Liste {Math.Floor((i - position) / 20f) + 1}");
                    sb.AppendLine($"{i+1,3}. | {role.Name, -20} | {rm.Money,-3} {CurrencyName} | Priorität {rm.Priority}");
                    if ((i - position) % elementsPerList == elementsPerList - 1)
                    {
                        sb.Append("```");
                        rankstrings.Add(sb.ToString());
                        sb.Clear();
                    }
                }

                if (sb.Length > 0)
                {
                    sb.Append("```");
                    rankstrings.Add(sb.ToString());
                    sb.Clear();
                }

                var channel = count <= 20 ? Context.Channel : await Context.User.GetOrCreateDMChannelAsync();

                foreach (var s in rankstrings)
                {
                    await channel.SendMessageAsync(s);
                    Thread.Sleep(250);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Payroll([Remainder]int count = 20) => await Payroll(count, 1);
        }
    }
}
