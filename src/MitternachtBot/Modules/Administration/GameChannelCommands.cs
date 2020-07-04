using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Administration.Services;
using Mitternacht.Services;

namespace Mitternacht.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class GameChannelCommands : MitternachtSubmodule<GameVoiceChannelService>
        {
            private readonly DbService _db;

            public GameChannelCommands(DbService db)
            {
                _db = db;
            }

            [MitternachtCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireBotPermission(GuildPermission.MoveMembers)]
            public async Task GameVoiceChannel()
            {
                var vch = ((IGuildUser)Context.User).VoiceChannel;

                if (vch == null)
                {
                    await ReplyErrorLocalized("not_in_voice").ConfigureAwait(false);
                    return;
                }
                ulong? id;
                using (var uow = _db.UnitOfWork)
                {
                    var gc = uow.GuildConfigs.For(Context.Guild.Id);

                    if (gc.GameVoiceChannel == vch.Id)
                    {
                        Service.GameVoiceChannels.TryRemove(vch.Id);
                        id = gc.GameVoiceChannel = null;
                    }
                    else
                    {
                        if(gc.GameVoiceChannel != null)
                            Service.GameVoiceChannels.TryRemove(gc.GameVoiceChannel.Value);
                        Service.GameVoiceChannels.Add(vch.Id);
                        id = gc.GameVoiceChannel = vch.Id;
                    }

                    uow.SaveChanges();
                }

                if (id == null)
                {
                    await ReplyConfirmLocalized("gvc_disabled").ConfigureAwait(false);
                }
                else
                {
                    Service.GameVoiceChannels.Add(vch.Id);
                    await ReplyConfirmLocalized("gvc_enabled", Format.Bold(vch.Name)).ConfigureAwait(false);
                }
            }
        }
    }
}
