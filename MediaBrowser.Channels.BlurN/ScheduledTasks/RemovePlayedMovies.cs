using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Channels.BlurN.Helpers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Common.Net;

namespace MediaBrowser.Channels.BlurN.ScheduledTasks
{
    class RemovePlayedMovies : IScheduledTask
    {
        private readonly IApplicationHost _appHost;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IHttpClient _httpClient;

        public RemovePlayedMovies(IHttpClient httpClient, IApplicationHost appHost, IServerConfigurationManager serverConfigurationManager, IUserManager userManager, IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _serverConfigurationManager = serverConfigurationManager;
            _userManager = userManager;
            _json = json;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
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
                return "Remove movies from the BlurN channel which exist in your library and are marked as played by all users.";
            }
        }

        public string Key
        {
            get
            {
                return "BlurNRemoveWatchedMovies";
            }
        }

        public string Name
        {
            get
            {
                return "Remove watched movies";
            }
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Tracking.Track(_httpClient, _appHost, _serverConfigurationManager, "start", "removeplayed", cancellationToken).ConfigureAwait(false);

            var config = Plugin.Instance.Configuration;

            if (config.HidePlayedMovies)
            {
                IEnumerable<BaseItem> library;
                Dictionary<string, BaseItem> libDict = new Dictionary<string, BaseItem>();

                Plugin.DebugLogger($"User count is {_userManager.Users.Count()}");

                library = _libraryManager.GetItemList(new InternalItemsQuery() { HasImdbId = true, SourceTypes = new SourceType[] { SourceType.Library } });

                Plugin.DebugLogger($"Library count is {library.Count()}");

                foreach (BaseItem libItem in library)
                {
                    bool isPlayedByAll = true;
                    foreach (User user in _userManager.Users)
                    {
                        if (!libItem.IsPlayed(user))
                        {
                            Plugin.DebugLogger($"Movie {libItem.OriginalTitle} not played by user {user.Name}");

                            isPlayedByAll = false;
                            break;
                        }
                    }

                    if (isPlayedByAll)
                    {
                        Plugin.DebugLogger($"Movie {libItem.OriginalTitle} played by all users");

                        string libIMDbId = libItem.GetProviderId(MetadataProviders.Imdb);
                        if (!libDict.ContainsKey(libIMDbId))
                            libDict.Add(libIMDbId, libItem);
                    }
                }

                Plugin.DebugLogger($"Watched movie count is {libDict.Count}");

                if (libDict.Count > 0)
                {
                    string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

                    if (_fileSystem.FileExists(dataPath))
                    {
                        var existingData = _json.DeserializeFromFile<List<BlurNItem>>(dataPath);

                        if (existingData != null)
                        {
                            bool removedItems = false;
                            for (int ci = 0; ci < existingData.Count; ci++)
                            {
                                BlurNItem channelItem = existingData[ci];
                                BaseItem libraryItem = libDict.FirstOrDefault(i => i.Key == channelItem.ImdbId).Value;
                                if (libraryItem != default(BaseItem))
                                {
                                    existingData.RemoveAt(ci);
                                    ci--;
                                    removedItems = true;

                                    Plugin.DebugLogger($"Removing watched movie {libraryItem.OriginalTitle} from BlurN channel");
                                }
                            }

                            if (removedItems)
                            {
                                Plugin.DebugLogger("Saving updated BlurN database");

                                _json.SerializeToFile(existingData, dataPath);
                            }
                        }
                    }
                }
            }
            else
                Plugin.DebugLogger("Did not remove played movies due to configuration setting.");

            await Tracking.Track(_httpClient, _appHost, _serverConfigurationManager, "end", "removeplayed", cancellationToken).ConfigureAwait(false);

            progress.Report(100);
            return;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Until we can vary these default triggers per server and MBT, we need something that makes sense for both
            return new[] { 
            
                // At startup
                //new TaskTriggerInfo {Type = TaskTriggerInfo.TriggerStartup},

                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(12).Ticks}
            };
        }
    }
}
