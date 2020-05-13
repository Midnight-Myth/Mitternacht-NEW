using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MinecraftQuery;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;

namespace Mitternacht.Modules.Minecraft
{
    [Group]
    public class Minecraft : MitternachtTopLevelModule
    {
		private readonly MojangApi _mojangApi;

		public Minecraft(MojangApi mojangApi) {
			_mojangApi = mojangApi;
		}

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MinecraftUsernames(string username, DateTime? date = null)
        {
            try
            {
                var accountinfo = await _mojangApi.GetAccountInfoAsync(username, date).ConfigureAwait(false);
                var accountnames = await _mojangApi.GetAllAccountNamesAsync(accountinfo.Uuid).ConfigureAwait(false);

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
                var accountinfo = await _mojangApi.GetAccountInfoAsync(username, date).ConfigureAwait(false);
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
                var status = await _mojangApi.GetServiceStatusAsync().ConfigureAwait(false);
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

        private static string GetEmojiStringFromServiceStatus(ServiceStatus status)
            => status == ServiceStatus.Green ? ":white_check_mark:" :
                status == ServiceStatus.Yellow ? ":warning:" : ":x:";

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MinecraftServerStatus(string address = "gommehd.net:25565")
        {
            var split = address.Split(':');
            var host = split[0];
            ushort port = 25565;
            if (split.Length > 1) ushort.TryParse(split[1], out port);
            try
            {
                var sr = await ServerInfo.GetServerStatusAsync(host, port).ConfigureAwait(false);
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle(GetText("server_title", sr.HostAddress))
                    .AddField(GetText("server_status"), sr.ServerAvailable ? "Online" : "Offline", true)
                    .AddField(GetText("server_port"), sr.HostPort, true);

                if (sr.ServerAvailable)
                    embed.WithOkColor()
                        .AddField(GetText("server_ping"), $"{sr.Ping}ms", true)
                        .AddField(GetText("server_version"), sr.Version, true)
                        .AddField(GetText("server_motd"), sr.MotD, true)
                        .AddField(GetText("server_players"), $"{sr.CurrentPlayers} / {sr.MaxPlayers}", true);
                else
                    embed.WithErrorColor();

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await ReplyErrorLocalized("error_server").ConfigureAwait(false);
            }
        }

        [MitternachtCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task MinecraftServerPing(string address = "gommehd.net:25565", uint count = 1)
        {
            if (count > 0x10) count = 0x10;
            if (count < 1) count = 1;

            var split = address.Split(':');
            var host = split[0];
            ushort port = 25565;
            if (split.Length > 1) ushort.TryParse(split[1], out port);
            try
            {
                var pr = await ServerInfo.PingServerAsync(host, port).ConfigureAwait(false);
                if (!pr.ServerAvailable)
                {
                    await ReplyErrorLocalized("ping_fail", pr.HostAddress, pr.HostPort).ConfigureAwait(false);
                    return;
                }

                if (count == 1)
                {
                    await ReplyConfirmLocalized("ping_success", $"{pr.HostAddress}:{pr.HostPort}", $"{pr.Ping}").ConfigureAwait(false);
                    return;
                }

                var pings = new List<long> {pr.Ping};
                for (var i = 1; i < count; i++)
                {
                    var prBuf = await ServerInfo.PingServerAsync(host, port, 1000).ConfigureAwait(false);
                    if(prBuf.ServerAvailable) pings.Add(pr.Ping);
                }

                var ping = pings.Average();
                await ReplyConfirmLocalized("ping_success_mult", $"{pr.HostAddress}:{pr.HostPort}", pings.Count, count, $"{ping:N2}");
            }
            catch (Exception)
            {
                await ReplyErrorLocalized("error_server").ConfigureAwait(false);
            }
        }
    }
}