using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
        private readonly Logger _log;
        private readonly Timer _timer;

        public event Func<IEnumerable<BirthDateModel>, Task> UsersBirthday;

        //private int _customTimeAccessCount = 0;

        public BirthdayService(DbService db, DiscordSocketClient client) {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;
            
            _timer = new Timer(60 * 1000); // 10*60*1000 ms = 10min
            _timer.Elapsed += TimerHandler;
            _timer.Start();
            UsersBirthday += OnUsersBirthday;
        }

        private async Task OnUsersBirthday(IEnumerable<BirthDateModel> birthdays) {
            using (var uow = _db.UnitOfWork) {
                //_log.Info($"{birthdays.Count()} birthdays today");

                var guildBirthdayRoles = uow.GuildConfigs
                    .GetAllGuildConfigs(_client.Guilds.Select(g => g.Id).ToList())
                    .Where(gc => gc.BirthdaysEnabled)
                    .Select(gc => (Guild: _client.GetGuild(gc.GuildId), BirthdayRoleId: gc.BirthdayRoleId, MessageChannelId: gc.BirthdayMessageChannelId, Message: gc.BirthdayMessage))
                    .ToList();

                foreach (var (guild, birthdayRoleId, msgChannelId, msg) in guildBirthdayRoles) {
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
                    if (msgChannelId.HasValue)
                    {
                        var ch = guild.GetTextChannel(msgChannelId.Value);
                        if (birthdayusers.Any() && ch != null)
                        {
                            await ch.SendMessageAsync(string.Format(msg, string.Join(", ", birthdayusers.Select(u => u.Mention).ToList()))).ConfigureAwait(false);
                        }
                    }
                }

                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private void TimerHandler(object sender, ElapsedEventArgs e) {
            using (var uow = _db.UnitOfWork) {
                var time = DateTime.Now;
                //var time = GetCustomTimeForTesting();
                var bc = uow.BotConfig.GetOrCreate();
                var lasttime = bc.LastTimeBirthdaysChecked;
                if (lasttime.IsOtherDate(time) && lasttime < time) {
                    var birthdays = uow.BirthDates.GetBirthdays(time).ToList();
                    UsersBirthday?.Invoke(birthdays);
                }
                bc.LastTimeBirthdaysChecked = time;
                uow.BotConfig.Update(bc);
                uow.Complete();
            }
        }

        //private DateTime GetCustomTimeForTesting()
        //{
        //    using (var uow = _db.UnitOfWork)
        //    {
        //        var bc = uow.BotConfig.GetOrCreate();
        //        DateTime time;
        //        bc.LastTimeBirthdaysChecked = _customTimeAccessCount % 2 == 0 ? new DateTime(2018, 3, 18) : DateTime.MinValue;
        //        time = _customTimeAccessCount % 2 == 0 ? new DateTime(2018, 3, 20) : new DateTime(2018, 3, 18);
        //        uow.BotConfig.Update(bc);
        //        uow.Complete();
        //        _customTimeAccessCount++;
        //        return time;
        //    }
        //}
    }
}