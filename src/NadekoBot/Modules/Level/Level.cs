using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Level.Services;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NadekoBot.Services.Database.Repositories.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Level
{
    [NadekoModule("Level")]
    public partial class Level : NadekoTopLevelModule<LevelService>
    {
        public static string CurrencyName { get; set; }
        public static string CurrencyPluralName { get; set; }
        public static string CurrencySign { get; set; }
        
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        public Level(IBotCredentials creds, DbService db)
        {
            _db = db;
            _creds = creds;

            using (var uow = _db.UnitOfWork)
            {
                var conf = uow.BotConfig.GetOrCreate();

                CurrencyName = conf.CurrencyName;
                CurrencySign = conf.CurrencySign;
                CurrencyPluralName = conf.CurrencyPluralName;
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rank([Remainder] IUser user = null)
        {
            user = user ?? Context.User;
            await Rank(user.Id);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rank(ulong userId)
        {
            LevelModel lm;
            int total = 0;
            int rank = 0;
            using (var uow = _db.UnitOfWork)
            {
                lm = uow.LevelModel.GetOrCreate(userId);
                total = uow.LevelModel.GetAll().Count();
                rank = uow.LevelModel.GetAll().OrderByDescending(p => p.TotalXP).ToList().IndexOf(lm) + 1;
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            
            if (userId == Context.User.Id)
            {
                await Context.Channel.SendMessageAsync($"{ Context.User.Mention }: **LEVEL { lm.Level } | XP { lm.CurrentXP }/{ LevelModelRepository.GetXPToLevel(lm.Level) } | TOTAL XP { lm.TotalXP } | RANK { rank }/{ total }**");
            }
            else
            {
                var user = (await Context.Guild.GetUsersAsync().ConfigureAwait(false)).FirstOrDefault(u => u.Id == userId);
                await Context.Channel.SendMessageAsync($"{ Context.User.Mention }: **{user?.Nickname ?? userId.ToString()}\'s Rang > LEVEL { lm.Level } | XP { lm.CurrentXP }/{ LevelModelRepository.GetXPToLevel(lm.Level) } | TOTAL XP { lm.TotalXP } | RANK { rank }/{ total }**");
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Ranks(int count, [Remainder]int position)
        {
            List<LevelModel> levelmodels = new List<LevelModel>();
            using (var uow = _db.UnitOfWork)
            {
                levelmodels = uow.LevelModel.GetAll().ToList();
                await uow.CompleteAsync().ConfigureAwait(false);
            }

            position--;
            if(position < 0) position = 0;
            

            levelmodels = levelmodels.OrderByDescending(p => p.TotalXP).ToList();
            if (count <= 0) count = levelmodels.Count;

            List<string> rankstrings = new List<string>();
            var sb = new StringBuilder();
            sb.AppendLine("__**Rangliste**__");
            for (int i = position; i < (levelmodels.Count > count ? count : levelmodels.Count) + position; i++)
            {
                var lm = levelmodels.ElementAt(i);
                var user = await Context.Guild.GetUserAsync(lm.UserId).ConfigureAwait(false);
                if (lm.TotalXP == 0) break;

                if ((i - position) % 20 == 0) sb.AppendLine($"```Liste {Math.Floor((i - position) / 20f) + 1}");
                if (lm.TotalXP > 0) sb.AppendLine($"{i + 1,3}. | {(user?.Username.TrimTo(24, true)) ?? lm.UserId.ToString().TrimTo(24,true), -26} | LEVEL {lm.Level,3} | XP {lm.CurrentXP,6}/{LevelModelRepository.GetXPToLevel(lm.Level),6} | TOTAL XP {lm.TotalXP,8}");
                if((i - position) % 20f == 1)
                {
                    sb.Append("```");
                    rankstrings.Add(sb.ToString());
                    sb.Clear();
                }
            }

            if(sb.Length > 0)
            {
                sb.Append("```");
                rankstrings.Add(sb.ToString());
                sb.Clear();
            }

            var channel = count <= 20 ? Context.Channel : await Context.User.GetOrCreateDMChannelAsync();

            foreach (var s in rankstrings)
            {
                await channel.SendMessageAsync(s);
                Thread.Sleep(250);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Ranks([Remainder] int count = 20)
        {
            await Ranks(count, 0);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task AddXP(int xp, [Remainder] IUser user = null)
        {
            user = user ?? Context.User;
            bool success = false;
            using (var uow = _db.UnitOfWork)
            {
                success = uow.LevelModel.TryAddXP(user.Id, xp);
                await uow.CompleteAsync().ConfigureAwait(false);
            }
            await Context.Channel.SendMessageAsync(success ? $"{Context.User.Mention}: {xp}XP an {user.Username} vergeben." : $"{Context.User.Mention}: Vergabe von {xp}XP an {user.Username} nicht möglich!");
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task TurnToXP(long moneyToSpend, [Remainder] IUser user = null)
        {
            user = user != null && _creds.IsOwner(Context.User) ? user : Context.User;
            if(moneyToSpend < 0)
            {
                await Context.Channel.SendMessageAsync($"Pech gehabt, {Context.User.Mention}, du kannst XP nicht in Geld zurückverwandeln.");
                return;
            }
            else if(moneyToSpend == 0)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, 0 {CurrencySign} umzuwandeln würde nichts bringen!");
                return;
            }
            using (var uow = _db.UnitOfWork)
            {
                var cur = uow.Currency.GetUserCurrency(user.Id);
                if(cur < moneyToSpend)
                {
                    await Context.Channel.SendMessageAsync(user == Context.User ? $"Du hast nicht genug Geld, {Context.User.Mention}!" : $"{Context.User.Mention}: {user.Username} hat nicht genügend Geld!");
                }
                else
                {
                    uow.LevelModel.TryAddXP(user.Id, (int)moneyToSpend * 5);
                    uow.Currency.TryUpdateState(user.Id, -moneyToSpend);
                    await Context.Channel.SendMessageAsync($"{Context.User.Mention}: {moneyToSpend}{CurrencySign} in {moneyToSpend * 5}XP umgewandelt" + (user != Context.User ? $" für {user.Username}" : ""));
                }
                await uow.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
