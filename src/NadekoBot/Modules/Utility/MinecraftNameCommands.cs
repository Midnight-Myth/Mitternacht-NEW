using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MinecraftQuery;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Services.Impl;

namespace Mitternacht.Modules.Utility
{
    public partial class Utility
    {
		[Group]
        public class MinecraftNameCommands : MitternachtSubmodule
        {
            private readonly MojangApi _mapi;

            public MinecraftNameCommands(MojangApiService mapis)
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
                            new EmbedBuilder().WithOkColor().WithTitle(GetText("mc_usernames_title", accountinfo.Name, names.Count))
                                .WithDescription(string.Join("\n", names.Skip(p * namesPerPage).Take(namesPerPage))),
                        pages-1,
                        reactUsers: new[] {Context.User as IGuildUser});
                }
                catch (Exception e)
                {
                    await ReplyErrorLocalized("mc_error", e.Message).ConfigureAwait(false);
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
                        .WithTitle(GetText("mc_pinfo_title", username))
                        .AddField("UUID", accountinfo.Uuid, true)
                        .AddField("Name", accountinfo.Name, true)
                        .AddField("Legacy", accountinfo.Legacy, true)
                        .AddField("Demo", accountinfo.Demo, true)
                        .WithTimestamp(date ?? DateTime.Now);
                    await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await ReplyErrorLocalized("mc_error", e.Message).ConfigureAwait(false);
                }
            }
        }
    }
    
}