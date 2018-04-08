using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Services;
using Mitternacht.Services.Database.Models;
using NLog;


namespace Mitternacht.Modules.Birthday.Services
{
    public class BirthdayService : INService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly Task _timerTask;

        private const int BirthdayCheckRepeatDelay = 60 * 1000;

        public event Func<List<BirthDateModel>, bool, Task> UsersBirthday = (b, d) => Task.CompletedTask;

        public BirthdayService(DbService db, DiscordSocketClient client)
        {
            _db = db;
            _client = client;

            _timerTask = Task.Run(async () =>
            {
                var log = LogManager.GetCurrentClassLogger();
                while (true)
                {
                    await Task.Delay(BirthdayCheckRepeatDelay);
                    try
                    {
                        await TimerHandler();
                    }
                    catch (Exception e)
                    {
                        log.Warn(e, CultureInfo.CurrentCulture, "Birthday Timer failed.");
                    }
                }
            });

            UsersBirthday += OnUsersBirthday;
        }

        private async Task OnUsersBirthday(List<BirthDateModel> birthdays, bool newDay)
        {
            using (var uow = _db.UnitOfWork)
            {
                //_log.Info($"{birthdays.Count()} birthdays today");

                var guildBirthdayRoles = uow.GuildConfigs
                    .GetAllGuildConfigs(_client.Guilds.Select(g => g.Id).ToList())
                    .Where(gc => gc.BirthdaysEnabled)
                    .Select(gc => (Guild: _client.GetGuild(gc.GuildId), BirthdayRoleId: gc.BirthdayRoleId, MessageChannelId: gc.BirthdayMessageChannelId, Message: gc.BirthdayMessage))
                    .ToList();

                //maybe running in parallel?
                foreach (var (guild, birthdayRoleId, msgChannelId, msg) in guildBirthdayRoles)
                {
                    var birthdayusers = birthdays.Select(b => guild.GetUser(b.UserId)).Where(u => u != null).ToList();

                    //birthday role handling
                    if (birthdayRoleId.HasValue)
                    {
                        var br = guild.GetRole(birthdayRoleId.Value);
                        if (br != null)
                        {
                            var oldBirthdayRoleMembers = (await br.GetMembersAsync().ConfigureAwait(false)).ToList();
                            //Remove birthday role from all who currently have it
                            foreach (var gu in oldBirthdayRoleMembers.Where(u => birthdayusers.All(gu => gu.Id != u.Id)).ToList())
                            {
                                await gu.RemoveRoleAsync(br).ConfigureAwait(false);
                            }

                            //add birthday role to everyone having birthday
                            foreach (var gu in birthdayusers.Where(gu => oldBirthdayRoleMembers.All(u => u.Id != gu.Id)).ToList())
                            {
                                await gu.AddRoleAsync(br).ConfigureAwait(false);
                            }
                        }
                    }

                    //birthday message handling
                    if (newDay && msgChannelId.HasValue)
                    {
                        var ch = guild.GetTextChannel(msgChannelId.Value);
                        if (birthdayusers.Any() && ch != null)
                        {
                            await ch.SendMessageAsync(string.Format(msg, string.Join(", ", birthdayusers.Select(u => u.Mention).ToList()))).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task TimerHandler()
        {
            using (var uow = _db.UnitOfWork)
            {
                var time = DateTime.Now;
                //var time = CustomTimeForTesting;
                var birthdays = uow.BirthDates.GetBirthdays(time).ToList();
                var bc = uow.BotConfig.GetOrCreate();
                var newDay = bc.LastTimeBirthdaysChecked.IsOtherDate(time);
                bc.LastTimeBirthdaysChecked = time;
                uow.BotConfig.Update(bc);
                await uow.CompleteAsync().ConfigureAwait(false);

                await UsersBirthday.Invoke(birthdays, newDay).ConfigureAwait(false);
            }
        }

        //private int _customTimeAccessCount = 0;
        //private DateTime CustomTimeForTesting
        //{
        //    get
        //    {
        //        using (var uow = _db.UnitOfWork)
        //        {
        //            var bc = uow.BotConfig.GetOrCreate();
        //            bc.LastTimeBirthdaysChecked = _customTimeAccessCount % 2 == 0 ? DateTime.Today : DateTime.MinValue;
        //            var time = _customTimeAccessCount % 2 == 0 ? DateTime.Today.AddDays(2) : DateTime.Today;
        //            uow.BotConfig.Update(bc);
        //            uow.Complete();
        //            _customTimeAccessCount++;
        //            return time;
        //        }
        //    }
        //}
    }
}