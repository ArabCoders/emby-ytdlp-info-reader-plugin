using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using MediaBrowser.Model.Logging;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;

namespace YTINFOReader.Helpers
{
    public class Utils
    {
#nullable enable
        public static ILogger? Logger { get; set; }
#nullable disable
        /// <summary>
        /// Regex for matching channel id.
        /// </summary>
        public static readonly Regex RX_C = new(Constants.CHANNEL_RX, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// Regex for matching playlist id.
        /// </summary>
        public static readonly Regex RX_P = new(Constants.PLAYLIST_RX, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// Regex for matching video id.
        /// </summary>
        public static readonly Regex RX_V = new(Constants.VIDEO_RX, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// JSON options for deserializing data.
        /// </summary>
        public static readonly JsonSerializerOptions JSON_OPTS = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        };

        /// <summary>
        /// Returns a 4-digit integer based on the SHA-256 hash of the basename.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method is used to generate a unique 4-digit integer for the episode index number.
        /// The method takes the basename of the file path, removes the extension, and converts it to lowercase.
        /// It then hashes the basename using SHA-256, converts the hash to a hexadecimal string, and then to ASCII values.
        /// The final 4 digits are then extracted and converted to an integer.
        /// If the ASCII string is less than 4 digits, it is padded with '9' characters.
        /// If the ASCII string is more than 4 digits, the first 4 digits are used.
        /// The method is deterministic and will always return the same 4-digit integer for the same basename.
        /// The method is case-insensitive and will return the same 4-digit integer for the same basename regardless of case.
        /// The method is designed to be unique for different basenames, but collisions are possible.
        /// The extended id is mainly used to differentiate between episodes with the same index number.
        /// </remarks>
        public static int ExtendId(string path)
        {
            string basename = Path.GetFileNameWithoutExtension(path).ToLower();
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(basename));
            string hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            StringBuilder asciiValues = new StringBuilder();
            foreach (char c in hex)
            {
                asciiValues.Append(((int)c).ToString());
            }
            string asciiString = asciiValues.ToString();
            string fourDigitString = asciiString.Length >= 4 ? asciiString.Substring(0, 4) : asciiString.PadRight(4, '9');
            int fourDigitNumber = int.Parse(fourDigitString);

            return fourDigitNumber;
        }

        /// <summary>
        /// Returns the Youtube ID from the file path.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method is used to extract the Youtube ID from the file path.
        /// The method first checks if the file path matches the channel ID regex.
        /// If the file path matches the channel ID regex, the method extracts the channel ID.
        /// If the file path matches the playlist ID regex, the method extracts the playlist ID.
        /// If the file path matches the video ID regex, the method extracts the video ID.
        /// If the file path does not match any regex, the method returns an empty string.
        /// The method is case-insensitive and will return the Youtube ID regardless of case.
        /// </remarks>
        public static string GetYTID(string name)
        {
            if (RX_C.IsMatch(name))
            {
                MatchCollection match = RX_C.Matches(name);
                return match[0].Groups["id"].ToString();
            }

            if (RX_P.IsMatch(name))
            {
                MatchCollection match = RX_P.Matches(name);
                return match[0].Groups["id"].ToString();
            }

            if (RX_V.IsMatch(name))
            {
                MatchCollection match = RX_V.Matches(name);
                return match[0].Groups["id"].ToString();
            }
            return "";
        }
        
        public static bool IsYouTubeContent(string name)
        {
            return GetYTID(name) != "";
        }

        /// <summary>
        /// Creates a person object of type director for the provided name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel_id"></param>
        /// <returns></returns>
        public static PersonInfo CreatePerson(string name, string channel_id)
        {
            return new PersonInfo
            {
                Name = name,
                Type = PersonType.Director,
                ProviderIds = new ProviderIdDictionary(new Dictionary<string, string> { { Constants.PLUGIN_NAME, channel_id } }),
            };
        }

        /// <summary>
        /// Returns path to where metadata json file should be.
        /// </summary>
        /// <param name="appPaths"></param>
        /// <param name="youtubeID"></param>
        /// <returns></returns>
        public static string GetVideoInfoPath(IServerApplicationPaths appPaths, string youtubeID)
        {
            var dataPath = Path.Combine(appPaths.CachePath, Constants.PLUGIN_NAME, youtubeID);
            return Path.Combine(dataPath, "ytvideo.info.json");
        }

        /// <summary>
        /// Reads JSON data from file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="metaFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static YTDLData ReadYTDLInfo(string path, string metaFile, CancellationToken? cancellationToken = null)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            YTDLData data = JsonSerializer.Deserialize<YTDLData>(File.ReadAllText(metaFile), JSON_OPTS);
            data.Path = path;
            return data;
        }

        /// <summary>
        /// Provides a Movie Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Movie> YTDLJsonToMovie(YTDLData json)
        {
            var item = new Movie();
            var result = new MetadataResult<Movie>
            {
                HasMetadata = true,
                Item = item
            };
            result.Item.Name = json.Title.Trim();
            result.Item.Overview = json.Description.Trim();
            var date = new DateTime(1970, 1, 1);
            try
            {
                date = DateTime.ParseExact(json.Upload_date, "yyyyMMdd", null);
            }
            catch { }
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(CreatePerson(json.Uploader.Trim(), json.Channel_id));
            result.Item.ProviderIds.Add(Constants.PLUGIN_NAME, json.Id);

            return result;
        }
        
        /// <summary>
        /// Provides a MusicVideo Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<MusicVideo> YTDLJsonToMusicVideo(YTDLData json)
        {
            var item = new MusicVideo();
            var result = new MetadataResult<MusicVideo>
            {
                HasMetadata = true,
                Item = item
            };
            result.Item.Name = string.IsNullOrEmpty(json.Track) ? json.Title.Trim() : json.Track.Trim();
            result.Item.Artists = new List<string> { !string.IsNullOrEmpty(json.Artist) ? json.Artist : json.Channel }.ToArray();
            if (!string.IsNullOrEmpty(json.Album))
            {
                result.Item.Album = json.Album;
            }

            if (!string.IsNullOrEmpty(json.Track))
            {
                result.Item.Name = json.Track;
            }

            result.Item.Overview = json.Description.Trim();
            var date = new DateTime(1970, 1, 1);
            try
            {
                date = DateTime.ParseExact(json.Upload_date, "yyyyMMdd", null);
            }
            catch { }
            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            result.AddPerson(CreatePerson(json.Uploader.Trim(), json.Channel_id));
            result.Item.ProviderIds.Add(Constants.PLUGIN_NAME, json.Id);

            return result;
        }

        /// <summary>
        /// Provides a Episode Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MetadataResult<Episode> YTDLJsonToEpisode(YTDLData json, string name = "")
        {
            Logger?.Debug($"{name}.YTDLJsonToEpisode: Processing '{json}'.");

            var item = new Episode();
            var result = new MetadataResult<Episode>
            {
                HasMetadata = true,
                Item = item
            };

            if (null == json.Upload_date)
            {
                Logger?.Warn($"{name}.YTDLJsonToEpisode: No upload date found for '{json.Id}' - '{json.Title}'. This most likely indicates the info.json file is corrupted. or was downloading when the video was deleted.");
            }

            var date = new DateTime(1970, 1, 1);
            if (null != json.Upload_date || null != json.Epoch)
            {
                try
                {
                    if (null != json.Upload_date)
                    {
                        date = DateTime.ParseExact(json.Upload_date, "yyyyMMdd", null);
                    }
                    else
                    {
                        date = DateTimeOffset.FromUnixTimeSeconds(json.Epoch ?? new long()).DateTime;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(json.Title))
            {
                result.Item.Name = json.Title.Trim();
            }

            if (!string.IsNullOrEmpty(json.Description))
            {
                result.Item.Overview = json.Description.Trim();
            }

            result.Item.ProductionYear = date.Year;
            result.Item.PremiereDate = date;
            try
            {
                result.Item.SortName = date.ToString("yyyyMMdd") + "-" + result.Item.Name;
            }
            catch
            {
            }

            if (null != json.Uploader && null != json.Channel_id)
            {
                result.AddPerson(CreatePerson(json.Uploader.Trim(), json.Channel_id));
            }
            result.Item.IndexNumber = int.Parse("1" + date.ToString("MMdd"));
            result.Item.ParentIndexNumber = int.Parse(date.ToString("yyyy"));
            result.Item.ProviderIds.Add(Constants.PLUGIN_NAME, json.Id);

            if (!string.IsNullOrEmpty(json.Path))
            {
                result.Item.IndexNumber = int.Parse($"1{date:MMdd}{ExtendId(json.Path)}");
            }

            if (!result.Item.IndexNumber.HasValue)
            {
                Logger?.Error($"{name}.YTDLJsonToEpisode: No index number found for '{json.Id}' - '{json.Title}'.");
                return new MetadataResult<Episode> { HasMetadata = false };
            }

            Logger?.Info($"{name}.YTDLJsonToEpisode: Matched '{json.Id}' - '{json.Title}' to 'S{result.Item.ParentIndexNumber}E{result.Item.IndexNumber}'.");

            return result;
        }

        /// <summary>
        /// Provides a Series Metadata Result from a json object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MetadataResult<Series> YTDLJsonToSeries(YTDLData json)
        {
            var item = new Series();
            var result = new MetadataResult<Series>
            {
                HasMetadata = true,
                Item = item
            };

            var identifier = json.Channel_id;
            var nameEx = "[" + json.Id + "]";
            result.Item.Name = json.Title.Trim();
            result.Item.Overview = json.Description.Trim();

            if (RX_C.IsMatch(nameEx))
            {
                MatchCollection match = RX_C.Matches(nameEx);
                identifier = match[0].Groups["id"].ToString();
            }
            else
            {
                if (RX_P.IsMatch(nameEx))
                {
                    MatchCollection match = RX_P.Matches(nameEx);
                    identifier = match[0].Groups["id"].ToString();
                }
            }

            result.Item.ProviderIds.Add(Constants.PLUGIN_NAME, identifier);
            return result;
        }

        /// <summary>
        /// Returns the path to the series info.json file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetSeriesInfo(string path)
        {
            // -- check if the path is a directory
            if (!Directory.Exists(path))
            {
                return "";
            }

            Matcher matcher = new();
            matcher.AddInclude("*.info.json");

            string infoPath = "";

            foreach (string file in matcher.GetResultsInFullPath(path))
            {
                if (RX_C.IsMatch(file) || RX_P.IsMatch(file))
                {
                    infoPath = file;
                    break;
                }
            }

            return infoPath;
        }
    }
}
