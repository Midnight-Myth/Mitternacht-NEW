﻿namespace Mitternacht.Services.Database.Models
{
    public class VoiceChannelStats : DbEntity
    {
        public ulong UserId { get; set; }
        public double TimeInVoiceChannel { get; set; }
    }
}
