using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Channels.BlurN.Helpers;
using MediaBrowser.Model.Notifications;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using System.IO;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class BlurNTasks
    {
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public BlurNTasks(IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _json = json;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public async Task ResetDatabase()
        {
            var config = Plugin.Instance.Configuration;
            config.Items = new OMDBList();
            Plugin.Instance.SaveConfiguration();

            string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

            if (_fileSystem.FileExists(dataPath))
                _json.SerializeToFile(config.Items.List, dataPath);

            if (config.EnableDebugLogging)
                Plugin.Logger.Debug("BlurN database reset actualized.");

            return;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new List<TaskTriggerInfo>();
    }
}
