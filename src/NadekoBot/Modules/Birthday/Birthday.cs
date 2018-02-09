using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Modules.Birthday.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Birthday
{
    public class Birthday : MitternachtTopLevelModule<BirthdayService>
    {
        private readonly DbService _db;
        private readonly IBotCredentials _bc;

        public Birthday(DbService db, IBotCredentials bc) {
            _db = db;
            _bc = bc;
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task BirthdaySet(IBirthDate bd) {
            using (var uow = _db.UnitOfWork) {
                var bdm = uow.BirthDates.GetUserBirthDate(Context.User.Id);
                if (!_bc.IsOwner(Context.User) && bdm?.Year != null || bdm != null && bdm.Year == null && (bdm.Day != bd.Day || bdm.Month != bd.Month)) {
                    await ReplyErrorLocalized("set_before").ConfigureAwait(false);
                    return;
                }

                uow.BirthDates.SetBirthDate(Context.User.Id, bd);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await ReplyConfirmLocalized("set", bd.ToString()).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task BirthdaySet(IBirthDate bd, IUser user)
        {
            using (var uow = _db.UnitOfWork)
            {
                uow.BirthDates.SetBirthDate(user.Id, bd);
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            if (user == Context.User)
                await ConfirmLocalized("set", bd.ToString()).ConfigureAwait(false);
            else
                await ConfirmLocalized("set_owner", user.ToString(), bd.ToString()).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task BirthdayRemove(IUser user) {
            using (var uow = _db.UnitOfWork) {
                var success = uow.BirthDates.DeleteBirthDate(user.Id);
                if (success)
                    await ConfirmLocalized("removed", user.ToString()).ConfigureAwait(false);
                else
                    await ErrorLocalized("remove_failed", user.ToString()).ConfigureAwait(false);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task BirthdayGet(IUser user = null) {
            user = user ?? Context.User;
            using (var uow = _db.UnitOfWork) {
                var bdm = uow.BirthDates.GetUserBirthDate(user.Id);
                var age = bdm?.Year != null ? (int) Math.Floor((DateTime.Now - new DateTime(bdm.Year.Value, bdm.Month, bdm.Day)).TotalDays / 365.25) : 0;
                if (user == Context.User)
                    if (bdm == null)
                        await ReplyErrorLocalized("self_none").ConfigureAwait(false);
                    else if (bdm.Year == null)
                        await ReplyConfirmLocalized("self", bdm.ToString()).ConfigureAwait(false);
                    else
                        await ReplyConfirmLocalized("self_age", bdm.ToString(), age).ConfigureAwait(false);
                else if (bdm == null)
                    await ReplyErrorLocalized("user_none", user.ToString()).ConfigureAwait(false);
                else if(bdm.Year == null)
                    await ReplyConfirmLocalized("user", user.ToString(), bdm.ToString()).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("user_age", user.ToString(), bdm.ToString(), age).ConfigureAwait(false);
            }
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Birthdays(IBirthDate bd = null) {
            bd = bd ?? BirthDate.Today;
            List<BirthDateModel> birthdates;
            using (var uow = _db.UnitOfWork) {
                birthdates = uow.BirthDates.GetBirthdays(bd, true).ToList();
            }

            if (!birthdates.Any()) {
                await ConfirmLocalized("none_date", bd.ToString()).ConfigureAwait(false);
                return;
            }

            var eb = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(GetText("list_title", bd.ToString()))
                .WithDescription(string.Join("\n", from bdm in birthdates
                                 select $"- {Context.Client.GetUserAsync(bdm.UserId).GetAwaiter().GetResult()?.ToString() ?? bdm.UserId.ToString()}{(bdm.Year.HasValue && !bd.Year.HasValue ? $"{BirthDate.Today.Year - bdm.Year}" : "")}"));
            await Context.Channel.EmbedAsync(eb).ConfigureAwait(false);
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task BirthdaysAll(int page = 1) {
            if (page < 1) page = 1;

            List<BirthDateModel> birthdates;
            using (var uow = _db.UnitOfWork) {
                birthdates = uow.BirthDates.GetAll().OrderBy(bdm => bdm.Month).ThenBy(bdm => bdm.Day).ToList();
            }

            if (!birthdates.Any()) {
                await ErrorLocalized("none").ConfigureAwait(false);
                return;
            }

            const int itemcount = 10;
            var pagecount = (int)Math.Ceiling(birthdates.Count / (itemcount * 1d));
            if (page > pagecount) page = pagecount;

            await Context.Channel.SendPaginatedConfirmAsync(Context.Client as DiscordSocketClient, page - 1, p => new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("all_title"))
                    .WithDescription(string.Join("\n", from bdm in birthdates.Skip(itemcount * p).Take(itemcount)
                        select $"- {Context.Client.GetUserAsync(bdm.UserId) .GetAwaiter() .GetResult() ?.ToString() ?? bdm.UserId.ToString()} - **{bdm.ToString()}**")), pagecount - 1, reactUsers: new []{Context.User as IGuildUser})
                .ConfigureAwait(false);
        }
    }
}
