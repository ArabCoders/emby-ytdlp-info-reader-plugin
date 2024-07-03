using System.Collections.Generic;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities.TV;
using YTINFOReader.Helpers;
using System.IO;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.FileSystemGlobbing;
using MediaBrowser.Model.Configuration;

namespace YTINFOReader.Provider
{
    public class LocalImageProvider : ILocalImageFileProvider, IHasOrder
    {
        public string Name => Constants.PLUGIN_NAME;
        public int Order => 0;
        private readonly ILogger _logger;

        private readonly Matcher _matcher = new Matcher();

        private readonly string[] _extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".tiff", ".gif", ".jp2" };

        public LocalImageProvider(ILogger logger)
        {
            _logger = logger;
            Utils.Logger = logger;

            foreach (var extension in _extensions)
            {
                _matcher.AddInclude($"**/*{extension}");
            }
        }

        public bool Supports(BaseItem item) => item is Movie || item is Episode || item is MusicVideo || item is Series;

        /// <summary>
        /// Retrieves Image.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="directoryService"></param>
        /// <returns></returns>
        public List<LocalImageInfo> GetImages(BaseItem item, LibraryOptions libraryOptions, IDirectoryService directoryService)
        {
            var list = new List<LocalImageInfo>(1);

            _logger.Debug($"{Name}.GetImages: Check if there is an image for '{item.Name}'.");


            if (string.IsNullOrEmpty(item.Path))
            {
                _logger.Debug($"{Name}.GetImages: {item.Name} - No path exists.");
                return list;
            }
            
            var imageFile = "";

            if (item is Series)
            {
                foreach (string file in _matcher.GetResultsInFullPath(item.Path))
                {
                    if (Utils.RX_C.IsMatch(file) || Utils.RX_P.IsMatch(file))
                    {
                        imageFile = file;
                        break;
                    }
                }
            }
            else
            {
                foreach (var extension in _extensions)
                {
                    var path = Path.ChangeExtension(item.Path, extension);
                    var file = directoryService.GetFile(path);
                    if (null != file && file.Exists)
                    {
                        imageFile = path;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(imageFile))
            {
                _logger.Debug($"{Name}.GetImages: No image found for '{item.Name}'.");
                return list;
            }

            list.Add(new LocalImageInfo
            {
                FileInfo = directoryService.GetFile(imageFile),
                Type = ImageType.Primary
            });

            return list;
        }
    }
}
