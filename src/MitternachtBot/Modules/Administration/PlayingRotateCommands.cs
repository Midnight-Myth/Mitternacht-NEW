using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;
using Mitternacht.Services.Database;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class PlayingRotateCommands : MitternachtSubmodule<PlayingRotateService>
        {
            private static readonly object _locker = new object();
            private readonly IUnitOfWork uow;

            public PlayingRotateCommands(IUnitOfWork uow)
            {
                this.uow = uow;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RotatePlaying()
            {
                var config = uow.BotConfig.GetOrCreate();

                var enabled = config.RotatingStatuses = !config.RotatingStatuses;
                uow.SaveChanges(false);

                if (enabled)
                    await ReplyConfirmLocalized("ropl_enabled").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("ropl_disabled").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task AddPlaying([Remainder] string status)
            {
                var config = uow.BotConfig.GetOrCreate();
                var toAdd = new PlayingStatus { Status = status };
                config.RotatingStatusMessages.Add(toAdd);
                await uow.SaveChangesAsync(false);

                await ReplyConfirmLocalized("ropl_added").ConfigureAwait(false);
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task ListPlaying()
            {
                if (!Service.BotConfig.RotatingStatusMessages.Any())
                    await ReplyErrorLocalized("ropl_not_set").ConfigureAwait(false);
                else
                {
                    var i = 1;
                    await ReplyConfirmLocalized("ropl_list",
                            string.Join("\n\t", Service.BotConfig.RotatingStatusMessages.Select(rs => $"`{i++}.` {rs.Status}")))
                        .ConfigureAwait(false);
                }

            }

            [MitternachtCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task RemovePlaying(int index)
            {
                index -= 1;

                var config = uow.BotConfig.GetOrCreate();

                if (index >= config.RotatingStatusMessages.Count)
                    return;
                var msg = config.RotatingStatusMessages[index].Status;
                config.RotatingStatusMessages.RemoveAt(index);
                await uow.SaveChangesAsync(false);

                await ReplyConfirmLocalized("reprm", msg).ConfigureAwait(false);
            }
        }
    }
}