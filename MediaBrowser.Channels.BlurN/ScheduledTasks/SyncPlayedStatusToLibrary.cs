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
using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Common.Net;

namespace MediaBrowser.Channels.BlurN.ScheduledTasks
{
    class SyncPlayedStatusToLibrary : IScheduledTask
    {
        private readonly IApplicationHost _appHost;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IHttpClient _httpClient;

        public SyncPlayedStatusToLibrary(IHttpClient httpClient, IApplicationHost appHost, IServerConfigurationManager serverConfigurationManager, IUserManager userManager, IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem, ILibraryManager libraryManager, IUserDataManager userDataManager)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _serverConfigurationManager = serverConfigurationManager;
            _json = json;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
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
                return "Syncs played status of movies on BlurN to library.";
            }
        }

        public string Key
        {
            get
            {
                return "BlurNSyncPlayedStatusToLibrary";
            }
        }

        public string Name
        {
            get
            {
                return "Sync played status to library";
            }
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Tracking.Track(_httpClient, _appHost, _serverConfigurationManager, "start", "syncplayed", cancellationToken);

            var config = Plugin.Instance.Configuration;
            bool debug = config.EnableDebugLogging;

            BlurNItems items = new BlurNItems();

            string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

            if (_fileSystem.FileExists(dataPath))
                items.List = _json.DeserializeFromFile<List<BlurNItem>>(dataPath);

            var users = _userManager.Users.ToList();

            if (debug)
                Plugin.Logger.Debug("[BlurN] Syncing played status of {0} users to library.", users.Count);

            for (int u = 0; u < users.Count; u++)
            {
                User user = users[u];
                Dictionary<string, BaseItem> libDict = Library.BuildLibraryDictionary(cancellationToken, _libraryManager, new InternalItemsQuery() { HasImdbId = true, User = user, IsPlayed = false, SourceTypes = new SourceType[] { SourceType.Library } });

                if (debug)
                    Plugin.Logger.Debug("[BlurN] User {0} has {1} unplayed movies in library.", user.Name, libDict.Count);

                for (int i = 0; i < items.List.Count; i++)
                {
                    BlurNItem blurNItem = items.List[i];
                    BaseItem libraryItem;
                    if (libDict.TryGetValue(blurNItem.ImdbId, out libraryItem))
                    {
                        UserItemData uid = _userDataManager.GetAllUserData(user.Id).FirstOrDefault(aud => aud.Key == config.ChannelRefreshCount + "-" + blurNItem.ImdbId);
                        if (uid != default(UserItemData))
                        {
                            if (uid.Played)
                            {
                                await libraryItem.MarkPlayed(user, uid.LastPlayedDate, true).ConfigureAwait(false);
                                if (debug)
                                    Plugin.Logger.Debug("[BlurN] Marked {0} as watched in movie library.", blurNItem.Title);
                            }
                        }
                    }

                    progress.Report(((u + ((i + 1) / items.List.Count)) / users.Count) * 100);
                }
            }

            Tracking.Track(_httpClient, _appHost, _serverConfigurationManager, "end", "syncplayed", cancellationToken);

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
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(1).Ticks}
            };
        }
    }
}
