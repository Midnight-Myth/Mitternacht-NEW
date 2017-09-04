using Discord;
using Discord.Commands;
using NadekoBot.Services;
using System;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Administration.Services;
using System.Linq;
using NadekoBot.Extensions;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class MuteCommands : NadekoSubmodule<MuteService>
        {
            private readonly DbService _db;

            public MuteCommands(DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(0)]
            public async Task SetMuteRole([Remainder] string name)
            {
                name = name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    config.MuteRoleName = name;
                    _service.GuildMuteRoles.AddOrUpdate(Context.Guild.Id, name, (id, old) => name);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                await ReplyConfirmLocalized("mute_role_set").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [Priority(1)]
            public Task SetMuteRole([Remainder] IRole role)
                => SetMuteRole(role.Name);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            [Priority(0)]
            public async Task Mute(IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_muted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            [Priority(1)]
            public async Task Mute(string time, IGuildUser user)
            {
                var argTime = time;
                int days, hours, minutes;
                string sdays = "0", shours = "0", sminutes = "0";


                if (argTime.Contains('d'))
                {
                    sdays = argTime.Split('d')[0];
                    argTime = argTime.Split('d')[1];
                }

                if (argTime.Contains('h'))
                {
                    shours = argTime.Split('h')[0];
                    argTime = argTime.Split('h')[1];
                }
                if (argTime.Contains('m'))
                {
                    sminutes = argTime.Split('m')[0];
                }

                days = Convert.ToInt32(sdays);
                hours = Convert.ToInt32(shours);
                minutes = Convert.ToInt32(sminutes);

                var muteTime = days * 24 * 60 + hours * 60 + minutes;
                try
                {
                    await _service.TimedMute(user, TimeSpan.FromMinutes(muteTime)).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_muted_time", Format.Bold(user.ToString()), muteTime).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            [Priority(1)]
            public async Task MuteTime(IGuildUser user) {
                if (user == null) return;
                var muteTime = _service.GetMuteTime(user);
                if (muteTime == null || muteTime.Value < DateTime.UtcNow) await Context.Channel.SendErrorAsync($"User {(string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname)} ist nicht gemutet.");
                else {
                    var ts = muteTime.Value - DateTime.UtcNow;
                    var tstring = $"{((int) Math.Floor(ts.TotalDays) == 0 ? "" : (int) Math.Floor(ts.TotalDays) + "d")} {(ts.Hours == 0 ? "" : ts.Hours + "h")} {(ts.Minutes == 0 ? "" : ts.Minutes + "min")} {(ts.Seconds == 0 ? "" : ts.Seconds + "s")}".Trim();
                    await Context.Channel.SendConfirmAsync($"User {(string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname)} ist noch {Format.Bold(tstring)} gemutet.");
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task MuteTime()
                => await MuteTime(Context.User as IGuildUser);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task Unmute(IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_unmuted", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            public async Task ChatMute(IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, MuteType.Chat).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_chat_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.KickMembers)]
            public async Task ChatUnmute(IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user, MuteType.Chat).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_chat_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task VoiceMute([Remainder] IGuildUser user)
            {
                try
                {
                    await _service.MuteUser(user, MuteType.Voice).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_voice_mute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task VoiceUnmute([Remainder] IGuildUser user)
            {
                try
                {
                    await _service.UnmuteUser(user, MuteType.Voice).ConfigureAwait(false);
                    await ReplyConfirmLocalized("user_voice_unmute", Format.Bold(user.ToString())).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("mute_error").ConfigureAwait(false);
                }
            }
        }
    }
}
