using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Customsearch.v1.Data;

namespace Mitternacht.Services
{
    public interface IGoogleApiService : IMService
    {
        IEnumerable<string> Languages { get; }

        Task<IEnumerable<string>> GetVideoLinksByKeywordAsync(string keywords, int count = 1);
        Task<IEnumerable<(string Name, string Id, string Url)>> GetVideoInfosByKeywordAsync(string keywords, int count = 1);
        Task<IEnumerable<string>> GetPlaylistIdsByKeywordsAsync(string keywords, int count = 1);
        Task<IEnumerable<string>> GetRelatedVideosAsync(string url, int count = 1);
        Task<IEnumerable<string>> GetPlaylistTracksAsync(string playlistId, int count = 50);
        Task<IReadOnlyDictionary<string, TimeSpan>> GetVideoDurationsAsync(IEnumerable<string> videoIds);
        Task<ImageResult> GetImageAsync(string query, int start = 1);
        Task<string> Translate(string sourceText, string sourceLanguage, string targetLanguage);

        Task<string> ShortenUrl(string url);
    }

    public struct ImageResult
    {
        public Result.ImageData Image { get; }
        public string Link { get; }

        public ImageResult(Result.ImageData image, string link)
        {
            Image = image;
            Link = link;
        }
    }
}
