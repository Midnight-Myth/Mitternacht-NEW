using MinecraftQuery;

namespace Mitternacht.Services.Impl
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