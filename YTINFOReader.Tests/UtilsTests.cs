using Xunit;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller;
using Moq;
using System.Collections.Generic;
using System;
using System.IO;
using YTINFOReader.Helpers;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace YTINFOReader.Tests
{
    public class UtilsTest
    {
        [Theory]
        [InlineData("Foo", "")]
        [InlineData("ChannelName - 20190113 - this is a test title [dQw4w9WgXcQ].mkv", "dQw4w9WgXcQ")]
        [InlineData("ChannelName - 20190113 - this is a test title [youtube-dQw4w9WgXcQ].mkv", "dQw4w9WgXcQ")]
        public void GetYouTubeVideoIds(string fn, string expected)
        {
            var result = "";
            if (Utils.RX_V.IsMatch(fn))
            {
                MatchCollection match = Utils.RX_V.Matches(fn);
                result = match[0].Groups["id"].ToString();
            }

            Assert.Equal(expected, result);
        }
        [Theory]
        [InlineData("Foo", "")]
        [InlineData("ChannelName Videos [UCuAXFkgsw1L7xaCfnd5JJOw].info.json", "UCuAXFkgsw1L7xaCfnd5JJOw")]
        [InlineData("ChannelName Videos [youtube-UCuAXFkgsw1L7xaCfnd5JJOw].info.json", "UCuAXFkgsw1L7xaCfnd5JJOw")]
        [InlineData("ChannelName Videos [youtube-HCuAXFkgsw1L7xaCfnd5JJOw].info.json", "HCuAXFkgsw1L7xaCfnd5JJOw")]
        public void GetYouTubeChannelIds(string fn, string expected)
        {
            var result = "";
            if (Utils.RX_C.IsMatch(fn))
            {
                MatchCollection match = Utils.RX_C.Matches(fn);
                result = match[0].Groups["id"].ToString();
            }

            Assert.Equal(expected, result);
        }
        [Theory]
        [InlineData("Foo", "")]
        [InlineData("Cool playlist [PLirUFnHsz2_v-j-wQfot7uYSeoRgFv0K0].info.json", "PLirUFnHsz2_v-j-wQfot7uYSeoRgFv0K0")]
        [InlineData("Cool playlist [youtube-PLirUFnHsz2_v-j-wQfot7uYSeoRgFv0K0].info.json", "PLirUFnHsz2_v-j-wQfot7uYSeoRgFv0K0")]
        [InlineData("Cool playlist using channel id [UCU_fSrQkYzBBe9jtep0EIIg].info.json", "")]
        [InlineData("Cool playlist using channel id [youtube-UCU_fSrQkYzBBe9jtep0EIIg].info.json", "")]
        public void GetYouTubePlaylistIds(string fn, string expected)
        {
            var result = "";
            if (Utils.RX_P.IsMatch(fn))
            {
                MatchCollection match = Utils.RX_P.Matches(fn);
                result = match[0].Groups["id"].ToString();
            }

            Assert.Equal(expected, result);
        }
        [Fact]
        public void CreatePersonTest()
        {
            var result = Utils.CreatePerson("Rick Astley", "UCuAXFkgsw1L7xaCfnd5JJOw");
            var expected = new PersonInfo
            {
                Name = "Rick Astley",
                Type = PersonType.Director,
                ProviderIds = new ProviderIdDictionary(new Dictionary<string, string> { { Constants.PLUGIN_NAME, "UCuAXFkgsw1L7xaCfnd5JJOw" } }),
            };

            Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(result));
        }
        [Fact]
        public void GetVideoInfoPathTest()
        {
            var mockAppPath = Mock.Of<IServerApplicationPaths>(a =>
                a.CachePath == Path.Combine("foo", "bar").ToString()
            );

            var result = Utils.GetVideoInfoPath(mockAppPath, "id123");
            Assert.Equal(Path.Combine("foo", "bar", Constants.PLUGIN_NAME, "id123", "ytvideo.info.json").ToString(), result);
        }
        [Fact]
        public void YTDLJsonToMovieTest()
        {
            var result = Utils.YTDLJsonToMovie(GetYouTubeVideoData());

            Assert.True(result.HasMetadata);
            Assert.Equal("Never Gonna Give You Up", result.Item.Name);
            Assert.Equal("The official video for “Never Gonna Give You Up” by Rick Astley", result.Item.Overview);
            Assert.Equal(2009, result.Item.ProductionYear);
            Assert.Equal("20091025", (result.Item.PremiereDate ?? DateTime.Now).ToString("yyyyMMdd"));
            Assert.Equal("Rick Astley", result.People[0].Name);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.People[0].ProviderIds[Constants.PLUGIN_NAME]);
            Assert.Equal("dQw4w9WgXcQ", result.Item.ProviderIds[Constants.PLUGIN_NAME]);
        }
        [Fact]
        public void YTDLJsonToMusicTest()
        {
            var result = Utils.YTDLJsonToMusicVideo(GetYouTubeVideoData());
            Assert.True(result.HasMetadata);
            Assert.Equal("Music", result.Item.Name);
            Assert.Equal("The official video for “Never Gonna Give You Up” by Rick Astley", result.Item.Overview);
            Assert.Equal(2009, result.Item.ProductionYear);
            Assert.Equal("20091025", (result.Item.PremiereDate ?? DateTime.Now).ToString("yyyyMMdd"));
            Assert.Equal("Rick Astley", result.People[0].Name);
            Assert.Equal("Rick Astley Tester", result.Item.Artists[0]);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.People[0].ProviderIds[Constants.PLUGIN_NAME]);
            Assert.Equal("dQw4w9WgXcQ", result.Item.ProviderIds[Constants.PLUGIN_NAME]);
        }

        [Fact]
        public void YTDLJsonToMusicTestWithOutMusicData()
        {
            var result = Utils.YTDLJsonToMusicVideo(GetYTMusicWithoutMusicData());
            Assert.True(result.HasMetadata);
            Assert.Equal("Never Gonna Give You Up", result.Item.Name);
            Assert.Equal("Rick Astley Test", result.Item.Artists[0]);
            Assert.Equal("The official video for “Never Gonna Give You Up” by Rick Astley", result.Item.Overview);
            Assert.Equal(2009, result.Item.ProductionYear);
            Assert.Equal("20091025", (result.Item.PremiereDate ?? DateTime.Now).ToString("yyyyMMdd"));
            Assert.Equal("Rick Astley", result.People[0].Name);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.People[0].ProviderIds[Constants.PLUGIN_NAME]);
            Assert.Equal("dQw4w9WgXcQ", result.Item.ProviderIds[Constants.PLUGIN_NAME]);
        }

        [Fact]
        public void YTDLJsonToEpisodeTest()
        {
            var result = Utils.YTDLJsonToEpisode(GetYouTubeVideoData());
            Assert.True(result.HasMetadata);
            Assert.Equal("Never Gonna Give You Up", result.Item.Name);
            Assert.Equal("The official video for “Never Gonna Give You Up” by Rick Astley", result.Item.Overview);
            Assert.Equal(2009, result.Item.ProductionYear);
            Assert.Equal("20091025", (result.Item.PremiereDate ?? DateTime.Now).ToString("yyyyMMdd"));
            Assert.Equal("Rick Astley", result.People[0].Name);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.People[0].ProviderIds[Constants.PLUGIN_NAME]);
            Assert.Equal(110252511, result.Item.IndexNumber);
            Assert.Equal(2009, result.Item.ParentIndexNumber);
            Assert.Equal("dQw4w9WgXcQ", result.Item.ProviderIds[Constants.PLUGIN_NAME]);
        }
        [Fact]
        public void YTDLJsonToSeriesTest()
        {
            var result = Utils.YTDLJsonToSeries(GetYouTubeChannelData());
            System.Diagnostics.Debug.WriteLine(JsonSerializer.Serialize(GetYouTubeChannelData()));
            Assert.True(result.HasMetadata);
            Assert.Equal("Rick Astley", result.Item.Name);
            Assert.Equal("Official YouTube channel for Rick Astley.", result.Item.Overview);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.Item.ProviderIds[Constants.PLUGIN_NAME]);
        }
        public static YTDLData GetYouTubeVideoData()
        {
            string jsonString = "{\"id\":\"dQw4w9WgXcQ\",\"uploader\":\"Rick Astley\",\"channel\":\"Rick Astley\",\"upload_date\":\"20091025\",\"title\":\"Never Gonna Give You Up\",\"description\":\"The official video for “Never Gonna Give You Up” by Rick Astley\",\"channel_id\":\"UCuAXFkgsw1L7xaCfnd5JJOw\",\"track\":\"Music\",\"artist\":\"Rick Astley Tester\",\"album\":null,\"epoch\":1673637911,\"file_path\":null,\"thumbnails\":null}";
            return JsonSerializer.Deserialize<YTDLData>(jsonString, Utils.JSON_OPTS) ?? new YTDLData();
        }

        public static YTDLData GetYTMusicWithoutMusicData()
        {
            string jsonString = "{\"id\":\"dQw4w9WgXcQ\",\"uploader\":\"Rick Astley\",\"channel\":\"Rick Astley Test\",\"upload_date\":\"20091025\",\"title\":\"Never Gonna Give You Up\",\"description\":\"The official video for “Never Gonna Give You Up” by Rick Astley\",\"channel_id\":\"UCuAXFkgsw1L7xaCfnd5JJOw\",\"epoch\":1673637911,\"file_path\":null,\"thumbnails\":null}";
            return JsonSerializer.Deserialize<YTDLData>(jsonString, Utils.JSON_OPTS) ?? new YTDLData();
        }

        public static YTDLData GetYouTubeChannelData()
        {
            string jsonString = "{\"id\":\"UCuAXFkgsw1L7xaCfnd5JJOw\",\"uploader\":\"Rick Astley\",\"channel\":\"Rick Astley\",\"upload_date\":null,\"title\":\"Rick Astley\",\"description\":\"Official YouTube channel for Rick Astley.\",\"channel_id\":\"UCuAXFkgsw1L7xaCfnd5JJOw\",\"track\":null,\"artist\":null,\"album\":null,\"epoch\":1673637911,\"file_path\":null,\"thumbnails\":null}";
            return JsonSerializer.Deserialize<YTDLData>(jsonString, Utils.JSON_OPTS) ?? new YTDLData();
        }
    }
}
