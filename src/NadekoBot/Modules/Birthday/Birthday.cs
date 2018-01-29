using System.Threading.Tasks;
using Mitternacht.Common.Attributes;
using Mitternacht.Modules.Birthday.Models;
using Mitternacht.Modules.Birthday.Services;

namespace Mitternacht.Modules.Birthday
{
    public class Birthday : MitternachtTopLevelModule<BirthdayService>
    {
        public Birthday() {
            
        }

        [MitternachtCommand, Usage, Description, Aliases]
        public async Task BirthdaySet(BirthDate bd) {
            await ReplyAsync(bd.ToString()).ConfigureAwait(false);
        }
    }
}
