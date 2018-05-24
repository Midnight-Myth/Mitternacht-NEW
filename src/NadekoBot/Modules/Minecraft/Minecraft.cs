using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MinecraftQuery;
using MinecraftQuery.Models;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Minecraft
{
	[Group]
    public class Minecraft : MitternachtTopLevelModule
    {
        private readonly MojangApi _mapi;

        public Minecraft(MojangApiService mapis)
        {
            _mapi = mapis.MojangApi;
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MinecraftUsernames(string username, DateTime? date = null)
        {
            try
            {
                var accountinfo = await _mapi.GetAccountInfoAsync(username, date).ConfigureAwait(false);
                var accountnames = await _mapi.GetAllAccountNamesAsync(accountinfo.Uuid).ConfigureAwait(false);

                var names = accountnames.Select(kv =>
                    kv.Key == DateTime.MinValue ? $"- {kv.Value}" : $"- {kv.Value} (> {kv.Key:dd.MM.yyyy})").Reverse().ToList();

                const int namesPerPage = 20;

                var pages = (int)Math.Ceiling(names.Count * 1d / namesPerPage);

                await Context.Channel.SendPaginatedConfirmAsync((DiscordSocketClient) Context.Client, 0, p =>
                        new EmbedBuilder().WithOkColor().WithTitle(GetText("usernames_title", accountinfo.Name, names.Count))
                            .WithDescription(string.Join("\n", names.Skip(p * namesPerPage).Take(namesPerPage))),
                    pages-1,
                    reactUsers: new[] {Context.User as IGuildUser});
            }
            catch (Exception e)
            {
                await ReplyErrorLocalized("error_names", e.Message).ConfigureAwait(false);
            }
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MinecraftPlayerInfo(string username, DateTime? date = null)
        {
            try
            {
                var accountinfo = await _mapi.GetAccountInfoAsync(username, date).ConfigureAwait(false);
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("pinfo_title", username))
                    .AddField("UUID", accountinfo.Uuid, true)
                    .AddField("Name", accountinfo.Name, true)
                    .AddField("Legacy", accountinfo.Legacy, true)
                    .AddField("Demo", accountinfo.Demo, true)
                    .WithTimestamp(date ?? DateTime.Now);
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ReplyErrorLocalized("error_player", e.Message).ConfigureAwait(false);
            }
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MojangApiStatus()
        {
            try
            {
                var status = await _mapi.GetServiceStatus().ConfigureAwait(false);
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("apistatus_title"))
                    .WithCurrentTimestamp()
                    .WithDescription(string.Join("\n", status.Where(p => p.Key.Id != null).Select(p => $"{GetEmojiStringFromServiceStatus(p.Value)} {p.Key.Name}").ToArray()));
                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ReplyErrorLocalized("error_apistatus", e.Message).ConfigureAwait(false);
            }
        }

        private string GetEmojiStringFromServiceStatus(ServiceStatus status)
            => status == ServiceStatus.Green ? ":white_check_mark:" :
                status == ServiceStatus.Yellow ? ":warning:" : ":x:";

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MinecraftServerStatus(string address)
        {
            var split = address.Split(':');
            var host = split[0];
            ushort port = 25565;
            if (split.Length > 1) ushort.TryParse(split[1], out port);
            try
            {
                var sp = new ServerPinger(host, port);
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle(GetText("server_title", sp.HostAddress))
                    .AddField(GetText("server_status"), sp.ServerAvailable ? "Online" : "Offline", true)
                    .AddField(GetText("server_port"), sp.HostPort, true);

                if (sp.ServerAvailable)
                    embed.WithOkColor()
                        .AddField(GetText("server_ping"), $"{sp.Ping}ms", true)
                        .AddField(GetText("server_version"), sp.Version, true)
                        .AddField(GetText("server_motd"), sp.MotD, true)
                        .AddField(GetText("server_players"), $"{sp.CurrentPlayers} / {sp.MaxPlayers}", true);
                else
                    embed.WithErrorColor();

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await ReplyErrorLocalized("error_server", e.Message).ConfigureAwait(false);
            }
        }
    }
}