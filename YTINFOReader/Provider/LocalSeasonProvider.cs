using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YTINFOReader.Helpers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections;

namespace YTINFOReader.Provider
{
    public class LocalSeasonProvider : ILocalMetadataProvider<Season>, IHasItemChangeMonitor
    {
        protected readonly ILogger _logger;
        public string Name => Constants.PLUGIN_NAME;
        public LocalSeasonProvider(ILogger logger)
        {
            _logger = logger;
            Utils.Logger = logger;
        }
        public Task<MetadataResult<Season>> GetMetadata(ItemInfo info, LibraryOptions LibraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.Debug($"{Name}.GetMetadata: Getting metadata for '{info.Name}' - '{info.Path}'.");
            return Task.FromResult(new MetadataResult<Season>()
            {
                Item = new Season
                {
                    Name = Path.GetFileNameWithoutExtension(info.Path)
                },
                HasMetadata = true
            });
        }

        public bool HasChanged(BaseItem item, LibraryOptions LibraryOptions, IDirectoryService directoryService)
        {
            if (item is not Season)
            {
                return false;
            }

            Matcher matcher = new();
            matcher.AddInclude("*.info.json");

            // create array with all matching files
            var files = new ArrayList();

            foreach (string file in matcher.GetResultsInFullPath(item.Path))
            {
                if (!Utils.IsYouTubeContent(file))
                {
                    continue;
                }
                files.Add(file);
            }

            if (files.Count == 0)
            {
                return false;
            }

            foreach (string file in files)
            {
                var fileInfo = directoryService.GetFile(file);

                if (!fileInfo.Exists)
                {
                    continue;
                }

                if (fileInfo.LastWriteTimeUtc.ToUniversalTime() > item.DateLastSaved.ToUniversalTime())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
