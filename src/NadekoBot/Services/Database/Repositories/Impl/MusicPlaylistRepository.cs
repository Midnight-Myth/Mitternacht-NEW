﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mitternacht.Services.Database.Models;

namespace Mitternacht.Services.Database.Repositories.Impl
{
    public class MusicPlaylistRepository : Repository<MusicPlaylist>, IMusicPlaylistRepository
    {
        public MusicPlaylistRepository(DbContext context) : base(context)
        {
        }

        public List<MusicPlaylist> GetPlaylistsOnPage(int num)
        {
            if (num < 1)
                throw new IndexOutOfRangeException();

            return _set.Skip((num - 1) * 20)
                .Take(20)
                .Include(pl => pl.Songs)
                .ToList();
        }

        public MusicPlaylist GetWithSongs(int id) => 
            _set.Include(mpl => mpl.Songs)
                .FirstOrDefault(mpl => mpl.Id == id);
    }
}
