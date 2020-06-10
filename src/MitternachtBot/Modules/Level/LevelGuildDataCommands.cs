using System;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Extensions;
using Mitternacht.Modules.Level.Common;
using Mitternacht.Services;

namespace Mitternacht.Modules.Level
{
    public partial class Level
    {
		[Group]
        public class LevelGuildDataCommands : MitternachtSubmodule
        {
            private readonly DbService _db;

            public LevelGuildDataCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task LevelGuildData(LevelGuildData data, double value)
            {
                var previous = 0d;
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);
                    switch (data)
                    {
                        case Common.LevelGuildData.TurnToXpMultiplier:
                            previous = gc.TurnToXpMultiplier;
                            gc.TurnToXpMultiplier = value;
                            break;
                        case Common.LevelGuildData.MessageXpCharCountMin:
                            previous = gc.MessageXpCharCountMin;
                            gc.MessageXpCharCountMin = (int) value;
                            break;
                        case Common.LevelGuildData.MessageXpCharCountMax:
                            previous = gc.MessageXpCharCountMax;
                            gc.MessageXpCharCountMax = (int) value;
                            break;
                        case Common.LevelGuildData.MessageXpTimeDifference:
                            previous = gc.MessageXpTimeDifference;
                            gc.MessageXpTimeDifference = value;
                            break;
                    }

                    uow.GuildConfigs.Update(gc);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                var round = data == Common.LevelGuildData.MessageXpCharCountMax ||
                            data == Common.LevelGuildData.MessageXpCharCountMin;
                await ConfirmLocalized("levelguilddata_changed", data.ToString(), round ? (int) previous : previous,
                    round ? (int) value : value).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task LevelGuildData(LevelGuildData data)
            {
                var value = 0d;
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);
                    switch (data)
                    {
                        case Common.LevelGuildData.TurnToXpMultiplier:
                            value = gc.TurnToXpMultiplier;
                            break;
                        case Common.LevelGuildData.MessageXpCharCountMin:
                            value = gc.MessageXpCharCountMin;
                            break;
                        case Common.LevelGuildData.MessageXpCharCountMax:
                            value = gc.MessageXpCharCountMax;
                            break;
                        case Common.LevelGuildData.MessageXpTimeDifference:
                            value = gc.MessageXpTimeDifference;
                            break;
                    }
                }

                await ConfirmLocalized("levelguilddata", data.ToString(), value).ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task LevelGuildDataChoices()
            {
                await Context.Channel.SendConfirmAsync(string.Join(", ", Enum.GetNames(typeof(LevelGuildData))))
                    .ConfigureAwait(false);
            }
        }
    }
}