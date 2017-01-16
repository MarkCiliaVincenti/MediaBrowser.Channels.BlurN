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

namespace MediaBrowser.Channels.BlurN.ScheduledTasks
{
    class ResetDatabase : IScheduledTask
    {
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public ResetDatabase(IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _json = json;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public string Category
        {
            get
            {
                return "BlurN";
            }
        }

        public string Description
        {
            get
            {
                return "Resets the BlurN database, retaining the settings.";
            }
        }

        public string Key
        {
            get
            {
                return "BlurNResetDatabase";
            }
        }

        public string Name
        {
            get
            {
                return "Reset BlurN database";
            }
        }


        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var config = Plugin.Instance.Configuration;
            config.LastPublishDate = DateTime.MinValue;
            config.Items = new OMDBList();
            Plugin.Instance.SaveConfiguration();

            string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

            if (_fileSystem.FileExists(dataPath))
                _json.SerializeToFile(config.Items.List, dataPath);            

            progress.Report(100);
            return;
        }



        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Until we can vary these default triggers per server and MBT, we need something that makes sense for both
            return new[] {             
                new TaskTriggerInfo {Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromDays(365).Ticks }
            };
        }
    }
}
