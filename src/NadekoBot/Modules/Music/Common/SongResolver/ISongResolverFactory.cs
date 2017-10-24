using System.Threading.Tasks;
using Mitternacht.Modules.Music.Common.SongResolver.Strategies;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Music.Common.SongResolver
{
    public interface ISongResolverFactory
    {
        Task<IResolveStrategy> GetResolveStrategy(string query, MusicType? musicType);
    }
}
