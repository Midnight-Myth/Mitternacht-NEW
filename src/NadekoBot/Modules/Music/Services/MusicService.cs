﻿using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Services.Database.Models;
using NLog;
using System.IO;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Services.Impl;
using NadekoBot.Services;
using NadekoBot.Modules.Music.Common;
using NadekoBot.Modules.Music.Common.Exceptions;
using NadekoBot.Modules.Music.Common.SongResolver;

namespace NadekoBot.Modules.Music.Services
{
    public class MusicService : INService
    {
        public const string MusicDataPath = "data/musicdata";

        private readonly IGoogleApiService _google;
        private readonly NadekoStrings _strings;
        private readonly ILocalization _localization;
        private readonly DbService _db;
        private readonly Logger _log;
        private readonly SoundCloudApiService _sc;
        private readonly IBotCredentials _creds;
        private readonly ConcurrentDictionary<ulong, float> _defaultVolumes;
        private readonly DiscordSocketClient _client;

        public ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        public MusicService(DiscordSocketClient client, IGoogleApiService google,
            NadekoStrings strings, ILocalization localization, DbService db,
            SoundCloudApiService sc, IBotCredentials creds, IEnumerable<GuildConfig> gcs)
        {
            _client = client;
            _google = google;
            _strings = strings;
            _localization = localization;
            _db = db;
            _sc = sc;
            _creds = creds;
            _log = LogManager.GetCurrentClassLogger();

            try { Directory.Delete(MusicDataPath, true); } catch { }

            _defaultVolumes = new ConcurrentDictionary<ulong, float>(gcs.ToDictionary(x => x.GuildId, x => x.DefaultMusicVolume));

            Directory.CreateDirectory(MusicDataPath);

            //_t = new Timer(_ => _log.Info(MusicPlayers.Count(x => x.Value.Current.Current != null)), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public float GetDefaultVolume(ulong guildId)
        {
            return _defaultVolumes.GetOrAdd(guildId, (id) =>
            {
                using (var uow = _db.UnitOfWork)
                {
                    return uow.GuildConfigs.For(guildId, set => set).DefaultMusicVolume;
                }
            });
        }

        public Task<MusicPlayer> GetOrCreatePlayer(ICommandContext context)
        {
            var gUsr = (IGuildUser)context.User;
            var txtCh = (ITextChannel)context.Channel;
            var vCh = gUsr.VoiceChannel;
            return GetOrCreatePlayer(context.Guild.Id, vCh, txtCh);
        }

        public async Task<MusicPlayer> GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh)
        {
            string GetText(string text, params object[] replacements) =>
                _strings.GetText(text, _localization.GetCultureInfo(textCh.Guild), "Music".ToLowerInvariant(), replacements);

            _log.Info("Checks");
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (textCh != null)
                {
                    await textCh.SendErrorAsync(GetText("must_be_in_voice")).ConfigureAwait(false);
                }
                throw new NotInVoiceChannelException();
            }
            _log.Info("Get or add");
            return MusicPlayers.GetOrAdd(guildId, _ =>
            {
                _log.Info("Getting default volume");
                var vol = GetDefaultVolume(guildId);
                _log.Info("Creating musicplayer instance");
                var mp = new MusicPlayer(this, _google, voiceCh, textCh, vol);

                IUserMessage playingMessage = null;
                IUserMessage lastFinishedMessage = null;

                _log.Info("Subscribing");
                mp.OnCompleted += async (s, song) =>
                {
                    try
                    {
                        lastFinishedMessage?.DeleteAfter(0);

                        try
                        {
                            lastFinishedMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                    .WithAuthor(eab => eab.WithName(GetText("finished_song")).WithMusicIcon())
                                    .WithDescription(song.PrettyName)
                                    .WithFooter(ef => ef.WithText(song.PrettyInfo)))
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnStarted += async (player, song) =>
                {
                    //try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); }
                    //catch
                    //{
                    //    // ignored
                    //}
                    var sender = player;
                    if (sender == null)
                        return;
                    try
                    {
                        playingMessage?.DeleteAfter(0);

                        playingMessage = await mp.OutputTextChannel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                                    .WithAuthor(eab => eab.WithName(GetText("playing_song", song.Index + 1)).WithMusicIcon())
                                                    .WithDescription(song.Song.PrettyName)
                                                    .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + song.Song.PrettyInfo)))
                                                    .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnPauseChanged += async (player, paused) =>
                {
                    try
                    {
                        IUserMessage msg;
                        if (paused)
                            msg = await mp.OutputTextChannel.SendConfirmAsync(GetText("paused")).ConfigureAwait(false);
                        else
                            msg = await mp.OutputTextChannel.SendConfirmAsync(GetText("resumed")).ConfigureAwait(false);

                        msg?.DeleteAfter(10);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                _log.Info("Done creating");
                return mp;
            });
        }

        public MusicPlayer GetPlayerOrDefault(ulong guildId)
        {
            if (MusicPlayers.TryGetValue(guildId, out var mp))
                return mp;
            else
                return null;
        }

        public async Task TryQueueRelatedSongAsync(SongInfo song, ITextChannel txtCh, IVoiceChannel vch)
        {
            var related = (await _google.GetRelatedVideosAsync(song.VideoId, 4)).ToArray();
            if (!related.Any())
                return;

            var si = await ResolveSong(related[new NadekoRandom().Next(related.Length)], _client.CurrentUser.ToString(), MusicType.YouTube);
            if (si == null)
                throw new SongNotFoundException();
            var mp = await GetOrCreatePlayer(txtCh.GuildId, vch, txtCh);
            mp.Enqueue(si);
        }

        public async Task<SongInfo> ResolveSong(string query, string queuerName, MusicType? musicType = null)
        {
            query.ThrowIfNull(nameof(query));

            ISongResolverFactory resolverFactory = new SongResolverFactory(_sc);
            var strategy = await resolverFactory.GetResolveStrategy(query, musicType);
            var sinfo = await strategy.ResolveSong(query);

            if (sinfo == null)
                return null;

            sinfo.QueuerName = queuerName;

            return sinfo;
        }

        public async Task DestroyAllPlayers()
        {
            foreach (var key in MusicPlayers.Keys)
            {
                await DestroyPlayer(key);
            }
        }

        public async Task DestroyPlayer(ulong id)
        {
            if (MusicPlayers.TryRemove(id, out var mp))
                await mp.Destroy();
        }



        //public Task<SongInfo> ResolveYoutubeSong(string query, string queuerName)
        //{
        //    _log.Info("Getting video");
        //    //var (link, video) = await GetYoutubeVideo(query);

        //    //if (video == null) // do something with this error
        //    //{
        //    //    _log.Info("Could not load any video elements based on the query.");
        //    //    return null;
        //    //}
        //    ////var m = Regex.Match(query, @"\?t=(?<t>\d*)");
        //    ////int gotoTime = 0;
        //    ////if (m.Captures.Count > 0)
        //    ////    int.TryParse(m.Groups["t"].ToString(), out gotoTime);

        //    //_log.Info("Creating song info");
        //    //var song = new SongInfo
        //    //{
        //    //    Title = video.Title.Substring(0, video.Title.Length - 10), // removing trailing "- You Tube"
        //    //    Provider = "YouTube",
        //    //    Uri = async () => {
        //    //        var vid = await GetYoutubeVideo(query);
        //    //        if (vid.Item2 == null)
        //    //            throw new HttpRequestException();

        //    //        return await vid.Item2.GetUriAsync();
        //    //    },
        //    //    Query = link,
        //    //    ProviderType = MusicType.YouTube,
        //    //    QueuerName = queuerName
        //    //};
        //    return GetYoutubeVideo(query, queuerName);
        //}

        //private async Task<SongInfo> GetYoutubeVideo(string query, string queuerName)
        //{


        //    //if (string.IsNullOrWhiteSpace(link))
        //    //{
        //    //    _log.Info("No song found.");
        //    //    return (null, null);
        //    //}
        //    //_log.Info("Getting all videos");
        //    //var allVideos = await Task.Run(async () => { try { return await _yt.GetAllVideosAsync(link).ConfigureAwait(false); } catch { return Enumerable.Empty<YouTubeVideo>(); } }).ConfigureAwait(false);
        //    //var videos = allVideos.Where(v => v.AdaptiveKind == AdaptiveKind.Audio);
        //    //var video = videos
        //    //    .Where(v => v.AudioBitrate < 256)
        //    //    .OrderByDescending(v => v.AudioBitrate)
        //    //    .FirstOrDefault();

        //    //return (link, video);
        //}
    }
}