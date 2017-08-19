using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using System.IO;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    public class BlurNTasks
    {
        public static async Task ResetDatabase(Configuration.PluginConfiguration config, IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            string dataPath = Path.Combine(appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");
            string failedDataPath = Path.Combine(appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Failed.json");

            if (fileSystem.FileExists(dataPath))
                json.SerializeToFile((new BlurNItems()).List, dataPath);

            if (fileSystem.FileExists(failedDataPath))
                json.SerializeToFile((new FailedBlurNList()).List, failedDataPath);

            Plugin.DebugLogger("Database reset actualized.");

            return;
        }

        public static async Task<Configuration.PluginConfiguration> CheckIfResetDatabaseRequested(CancellationToken cancellationToken, IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            var config = Plugin.Instance.Configuration;
            if (config.LastPublishDate.Equals(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Reset database requested
                await ResetDatabase(config, json, appPaths, fileSystem).ConfigureAwait(false);
                config = Plugin.Instance.Configuration;
            }

            return config;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new List<TaskTriggerInfo>();
    }
}
