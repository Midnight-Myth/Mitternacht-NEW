﻿using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Newtonsoft.Json;

namespace Mitternacht.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class XkcdCommands : MitternachtSubmodule
        {
            private const string XkcdUrl = "https://xkcd.com";

            [MitternachtCommand, Usage, Description, Aliases]
            [Priority(0)]
            public async Task Xkcd(string arg = null)
            {
                if (arg?.ToLowerInvariant().Trim() == "latest")
                {
                    using (var http = new HttpClient())
                    {
                        var res = await http.GetStringAsync($"{XkcdUrl}/info.0.json").ConfigureAwait(false);
                        var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
                        var embed = new EmbedBuilder().WithColor(MitternachtBot.OkColor)
                                                  .WithImageUrl(comic.ImageLink)
                                                  .WithAuthor(eab => eab.WithName(comic.Title).WithUrl($"{XkcdUrl}/{comic.Num}").WithIconUrl("http://xkcd.com/s/919f27.ico"))
                                                  .AddField(efb => efb.WithName(GetText("comic_number")).WithValue(comic.Num.ToString()).WithIsInline(true))
                                                  .AddField(efb => efb.WithName(GetText("date")).WithValue($"{comic.Month}/{comic.Year}").WithIsInline(true));
                        var sent = await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);

                        await Task.Delay(10000).ConfigureAwait(false);

                        await sent.ModifyAsync(m => m.Embed = embed.AddField(efb => efb.WithName("Alt").WithValue(comic.Alt.ToString()).WithIsInline(false)).Build());
                    }
                    return;
                }
                await Xkcd(new NadekoRandom().Next(1, 1750)).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [Priority(1)]
            public async Task Xkcd(int num)
            {
                if (num < 1)
                    return;

                using (var http = new HttpClient())
                {
                    var res = await http.GetStringAsync($"{XkcdUrl}/{num}/info.0.json").ConfigureAwait(false);

                    var comic = JsonConvert.DeserializeObject<XkcdComic>(res);
                    var embed = new EmbedBuilder().WithColor(MitternachtBot.OkColor)
                                                  .WithImageUrl(comic.ImageLink)
                                                  .WithAuthor(eab => eab.WithName(comic.Title).WithUrl($"{XkcdUrl}/{num}").WithIconUrl("http://xkcd.com/s/919f27.ico"))
                                                  .AddField(efb => efb.WithName(GetText("comic_number")).WithValue(comic.Num.ToString()).WithIsInline(true))
                                                  .AddField(efb => efb.WithName(GetText("date")).WithValue($"{comic.Month}/{comic.Year}").WithIsInline(true));
                    var sent = await Context.Channel.EmbedAsync(embed)
                                 .ConfigureAwait(false);

                    await Task.Delay(10000).ConfigureAwait(false);

                    await sent.ModifyAsync(m => m.Embed = embed.AddField(efb => efb.WithName("Alt").WithValue(comic.Alt.ToString()).WithIsInline(false)).Build());
                }
            }
        }

        public class XkcdComic
        {
            public int Num { get; set; }
            public string Month { get; set; }
            public string Year { get; set; }
            [JsonProperty("safe_title")]
            public string Title { get; set; }
            [JsonProperty("img")]
            public string ImageLink { get; set; }
            public string Alt { get; set; }
        }
    }
}
