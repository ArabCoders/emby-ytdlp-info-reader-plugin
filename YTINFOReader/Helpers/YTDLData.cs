using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Model.IO;
using System.Text.Json;

namespace YTINFOReader.Helpers
{
    /// <summary>
    /// Object that represent how data from yt-dlp should look like.
    /// </summary>
    public class YTDLData
    {
        // direct object id be it either channel id or video id
        public string Id { get; set; }
        public string Channel { get; set; }
        public string Uploader { get; set; }
        public string Upload_date { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Channel_id { get; set; }
        public string Track { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
#nullable enable
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? Epoch { get; set; }
        public string? Path { get; set; }
#nullable disable
        public List<ThumbnailInfo> Thumbnails { get; set; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
