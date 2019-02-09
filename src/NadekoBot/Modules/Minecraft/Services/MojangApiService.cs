using MinecraftQuery;
using Mitternacht.Services;

namespace Mitternacht.Modules.Minecraft.Services
{
    public class MojangApiService : INService
    {
        public readonly MojangApi MojangApi;

        public MojangApiService()
        {
            MojangApi = new MojangApi();
        }
    }
}