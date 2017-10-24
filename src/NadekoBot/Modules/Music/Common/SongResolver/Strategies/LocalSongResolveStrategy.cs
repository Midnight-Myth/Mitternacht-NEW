﻿using System.IO;
using System.Threading.Tasks;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Modules.Music.Common.SongResolver.Strategies
{
    public class LocalSongResolveStrategy : IResolveStrategy
    {
        public Task<SongInfo> ResolveSong(string query)
        {
            return Task.FromResult(new SongInfo
            {
                Uri = () => Task.FromResult("\"" + Path.GetFullPath(query) + "\""),
                Title = Path.GetFileNameWithoutExtension(query),
                Provider = "Local File",
                ProviderType = MusicType.Local,
                Query = query,
                Thumbnail = "https://cdn.discordapp.com/attachments/155726317222887425/261850914783100928/1482522077_music.png",
            });
        }
    }
}
