﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mitternacht.Extensions
{
    public static class MessageChannelExtensions
    {
        public static Task<IUserMessage> EmbedAsync(this IMessageChannel ch, EmbedBuilder embed, string msg = "")
            => ch.SendMessageAsync(msg, embed: embed);

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string title, string error, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithErrorColor().WithDescription(error)
                .WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb);
        }

        public static Task<IUserMessage> SendErrorAsync(this IMessageChannel ch, string error)
             => ch.SendMessageAsync("", embed: new EmbedBuilder().WithErrorColor().WithDescription(error));

        public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColor().WithDescription(text).WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute)) eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer)) eb.WithFooter(efb => efb.WithText(footer));
            return ch.SendMessageAsync("", embed: eb);
        }

        public static Task<IUserMessage> SendConfirmAsync(this IMessageChannel ch, string text)
             => ch.SendMessageAsync("", embed: new EmbedBuilder().WithOkColor().WithDescription(text));

        public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, string seed, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3)
        {
            var i = 0;
            return ch.SendMessageAsync($"{seed}```css\n{string.Join("\n", items.GroupBy(item => i++ / columns).Select(ig => string.Concat(ig.Select(howToPrint))))}```");
        }

        public static Task<IUserMessage> SendTableAsync<T>(this IMessageChannel ch, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3) =>
            ch.SendTableAsync("", items, howToPrint, columns);
        
        private static readonly IEmote ArrowLeft = new Emoji("⬅");
        private static readonly IEmote ArrowRight = new Emoji("➡");

        public static Task SendPaginatedConfirmAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, EmbedBuilder> pageFunc, int? lastPage = null, bool addPaginatedFooter = true) 
            => channel.SendPaginatedConfirmAsync(client, currentPage, x => Task.FromResult(pageFunc(x)), lastPage, addPaginatedFooter);

        public static async Task SendPaginatedConfirmAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, Task<EmbedBuilder>> pageFunc, int? lastPage = null, bool addPaginatedFooter = true)
        {
            var embed = await pageFunc(currentPage).ConfigureAwait(false);

            if (addPaginatedFooter)
                embed.AddPaginatedFooter(currentPage, lastPage);

            var msg = await channel.EmbedAsync(embed);

            if (lastPage == 0)
                return;


            await msg.AddReactionAsync(ArrowLeft).ConfigureAwait(false);
            await msg.AddReactionAsync(ArrowRight).ConfigureAwait(false);

            await Task.Delay(2000).ConfigureAwait(false);

            async void ChangePage(SocketReaction r) {
                try {
                    if (r.Emote.Name == ArrowLeft.Name) {
                        if (currentPage == 0)
                            return;
                        var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (r.Emote.Name == ArrowRight.Name) {
                        if (lastPage != null && !(lastPage > currentPage)) return;
                        var toSend = await pageFunc(++currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                }
                catch (Exception) {
                    //ignored
                }
            }

            using (msg.OnReaction(client, ChangePage, ChangePage))
            {
                await Task.Delay(30000).ConfigureAwait(false);
            }

            await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
        }

        public static Task SendPaginatedMessageAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, string> pageFunc, int? lastPage = null, bool addPaginatedFooter = true)
            => channel.SendPaginatedMessageAsync(client, currentPage, p => Task.FromResult(pageFunc(p)), lastPage, addPaginatedFooter);

        public static async Task SendPaginatedMessageAsync(this IMessageChannel channel, DiscordSocketClient client, int currentPage, Func<int, Task<string>> pageFunc, int? lastPage = null, bool addPaginatedFooter = true) {
            var text = await pageFunc(currentPage);

            if (addPaginatedFooter)
                text += lastPage == null ? $"\n{currentPage}" : $"\n{currentPage}/{lastPage}";

            var msg = await channel.SendMessageAsync(text);
            if (lastPage == 0) return;

            await msg.AddReactionAsync(ArrowLeft);
            await msg.AddReactionAsync(ArrowRight);

            await Task.Delay(2000);

            async void ChangePage(SocketReaction r) {
                try {
                    if (r.Emote.Name == ArrowLeft.Name) {
                        if (currentPage == 0) return;
                        var modtext = await pageFunc(--currentPage);
                        if (addPaginatedFooter)
                            modtext += lastPage == null ? $"\n{currentPage}" : $"\n{currentPage}/{lastPage}";
                        await msg.ModifyAsync(mp => mp.Content = modtext);
                    }
                    else if (r.Emote.Name == ArrowRight.Name) {
                        if (lastPage != null && !(lastPage > currentPage)) return;
                        var modtext = await pageFunc(++currentPage);
                        if (addPaginatedFooter)
                            modtext += lastPage == null ? $"\n{currentPage}" : $"\n{currentPage}/{lastPage}";
                        await msg.ModifyAsync(mp => mp.Content = modtext);
                    }
                }
                catch (Exception) {
                    //who needs exception handling?
                }
            }

            using (msg.OnReaction(client, ChangePage, ChangePage)) {
                await Task.Delay(30000);
            }
            await msg.RemoveAllReactionsAsync();
        }
    }
}
