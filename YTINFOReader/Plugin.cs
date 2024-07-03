using System;
using YTINFOReader.Helpers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;

namespace YTINFOReader
{
    public class Plugin : BasePlugin
    {
        public override string Name => Constants.PLUGIN_NAME;
        public static Plugin Instance { get; private set; }
        public override Guid Id => Guid.Parse(Constants.PLUGIN_GUID);
        private readonly ILogger _logger;
        public override string Description => "Parse yt-dlp info files.";
        public Plugin(ILogManager logManager)
        {
            _logger = logManager.GetLogger(Name);
            _logger.Info("YTINFOReader plugin is loaded.");
        }
    }
}
