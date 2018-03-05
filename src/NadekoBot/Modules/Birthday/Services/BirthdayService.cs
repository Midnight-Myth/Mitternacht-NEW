using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using Mitternacht.Extensions;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Services;
using NLog;

namespace Mitternacht.Modules.Birthday.Services
{
    public class BirthdayService : INService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly Logger _log;
        private readonly Timer _timer;

        public event Func<Task> UsersBirthday;

        public BirthdayService(DbService db, DiscordSocketClient client) {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;
            _log.Info("test");
            
            _timer = new Timer(10 * 60 * 1000);
            _timer.Elapsed += TimerHandler;
            _timer.Start();
            UsersBirthday += OnUsersBirthday;
        }

        private async Task OnUsersBirthday() {
            using (var uow = _db.UnitOfWork) {
                var birthdays = uow.BirthDates.GetBirthdays(BirthDate.Today).ToList();
                //_log.Info($"{birthdays.Count} birthdays today");

                var guildBirthdayRoles = uow.GuildConfigs
                    .GetAllGuildConfigs(_client.Guilds.Select(g => g.Id).ToList())
                    .Where(gc => gc.BirthdayRoleId.HasValue)
                    .Select(gc => (Guild: _client.GetGuild(gc.GuildId), BirthdayRoleId: gc.BirthdayRoleId.Value))
                    .ToList();

                foreach (var (guild, birthdayRoleId) in guildBirthdayRoles) {
                    var br = guild.GetRole(birthdayRoleId);
                    if (br == null) continue;
                    var birthdayusers = birthdays.Select(b => guild.GetUser(b.UserId)).Where(u => u != null).ToList();
                    var oldBirthdayRoleMembers = (await br.GetMembersAsync().ConfigureAwait(false)).ToList();
                    foreach (var gu in oldBirthdayRoleMembers.Where(u => birthdayusers.All(gu => gu.Id != u.Id)).ToList()) {
                        await gu.RemoveRoleAsync(br).ConfigureAwait(false);
                    }

                    foreach (var gu in birthdayusers.Where(gu => oldBirthdayRoleMembers.All(u => u.Id != gu.Id)).ToList()) {
                        await gu.AddRoleAsync(br).ConfigureAwait(false);
                    }
                }

                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }

        private void TimerHandler(object sender, ElapsedEventArgs e) {
            using (var uow = _db.UnitOfWork) {
                var time = e.SignalTime;
                var bc = uow.BotConfig.GetOrCreate();
                if (bc.LastTimeBirthdaysChecked.IsOtherDate(time) && bc.LastTimeBirthdaysChecked < time) {
                    UsersBirthday?.Invoke();
                }
                bc.LastTimeBirthdaysChecked = time;
                uow.BotConfig.Update(bc);
                uow.Complete();
            }
        }
    }
}