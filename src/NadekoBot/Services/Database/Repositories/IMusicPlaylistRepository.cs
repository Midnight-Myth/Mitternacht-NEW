using System.Collections.Generic;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories
{
    public interface IMusicPlaylistRepository : IRepository<MusicPlaylist>
    {
        List<MusicPlaylist> GetPlaylistsOnPage(int num);
        MusicPlaylist GetWithSongs(int id);
    }
}
