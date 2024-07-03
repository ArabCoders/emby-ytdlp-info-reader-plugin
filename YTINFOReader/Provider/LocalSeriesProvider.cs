using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using YTINFOReader.Helpers;

namespace YTINFOReader.Provider
{
    public class LocalSeriesProvider : AbstractLocalProvider<LocalSeriesProvider, Series>
    {
        public override string Name => Constants.PLUGIN_NAME;

        public LocalSeriesProvider(IFileSystem fileSystem, ILogger logger) : base(fileSystem, logger) { }

        internal override MetadataResult<Series> GetMetadataImpl(YTDLData jsonObj) => Utils.YTDLJsonToSeries(jsonObj);

        public override Task<MetadataResult<Series>> GetMetadata(ItemInfo info, LibraryOptions LibraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.Info($"{Name}.GetMetadata: Getting '{info.Name}' metadata from '{info.Path}'.");

            MetadataResult<Series> result = new();

            string metaFile = Utils.GetSeriesInfo(info.Path);
            if (string.IsNullOrEmpty(metaFile))
            {
                _logger.Debug($"{Name}.GetMetadata: No info.json file was found for '{info.Name}'.");
                return Task.FromResult(result);
            }

            _logger.Debug($"{Name}.GetMetadata: Found '{metaFile}' as info.json for '{info.Name}");

            var infoJson = Utils.ReadYTDLInfo(info.Path, metaFile, cancellationToken);
            result = GetMetadataImpl(infoJson);
            _logger.Debug($"{Name}.GetMetadata: Final resuts: '{result}'");

            return Task.FromResult(result);
        }
    }
}
