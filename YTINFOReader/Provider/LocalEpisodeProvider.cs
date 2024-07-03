using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities.TV;
using YTINFOReader.Helpers;

namespace YTINFOReader.Provider
{
    public class LocalEpisodeProvider : AbstractLocalProvider<LocalEpisodeProvider, Episode>
    {
        public LocalEpisodeProvider(IFileSystem fileSystem, ILogger logger) : base(fileSystem, logger) { }
        public override string Name => Constants.PLUGIN_NAME;
        internal override MetadataResult<Episode> GetMetadataImpl(YTDLData jsonObj)
        {
            return Utils.YTDLJsonToEpisode(jsonObj, Name);
        }
    }
}
