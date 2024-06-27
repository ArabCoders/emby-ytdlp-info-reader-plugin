using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YTINFOReader.Helpers;
using MediaBrowser.Model.Configuration;

namespace YTINFOReader.Provider
{
    public abstract class AbstractLocalProvider<B, T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor
    where T : BaseItem
    {
        protected readonly ILogger _logger;
        protected readonly IFileSystem _fileSystem;

        /// <summary>
        /// Providers name, this appears in the library metadata settings.
        /// </summary>
        public abstract string Name { get; }

        public AbstractLocalProvider(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            Utils.Logger = logger;
        }

        protected FileSystemMetadata GetInfoJson(string path)
        {
            _logger.Debug($"YIR GetInfoJson: {path}");
            var fileInfo = _fileSystem.GetFileSystemInfo(path);
            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path));
            var directoryPath = directoryInfo.FullName;
            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".info.json");
            var file = _fileSystem.GetFileInfo(specificFile);
            return file;
        }

        /// <summary>
        /// Returns bolean if item has changed since last recorded.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="directoryService"></param>
        /// <returns></returns>
        public virtual bool HasChanged(BaseItem item, LibraryOptions LibraryOptions, IDirectoryService directoryService)
        {
            var fileInfo = directoryService.GetFile(item.Path);

            if (!fileInfo.Exists)
            {
                _logger.Error($"YIR '{item.Path}' does not exist.");
                return true;
            }

            if (fileInfo.LastWriteTimeUtc.ToUniversalTime() > item.DateLastSaved.ToUniversalTime())
            {
                _logger.Info($"YIR '{item.Path}' has changed.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves metadata of item.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="directoryService"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<MetadataResult<T>> GetMetadata(ItemInfo info, LibraryOptions LibraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            _logger.Debug($"YIR GetMetadata: {info.Path}");
            var result = new MetadataResult<T>();
            var infoFile = Path.ChangeExtension(info.Path, "info.json");
            if (!File.Exists(infoFile))
            {
                return Task.FromResult(result);
            }
            var jsonObj = Utils.ReadYTDLInfo(infoFile, directoryService.GetFile(info.Path), cancellationToken);
            _logger.Debug($"YIR GetMetadata Result: {jsonObj}");
            result = GetMetadataImpl(jsonObj);

            return Task.FromResult(result);
        }

        internal abstract MetadataResult<T> GetMetadataImpl(YTDLData jsonObj);
    }
}
