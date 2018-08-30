﻿using MediaBrowser.Channels.BlurN.Helpers;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            await Tracking.Track(_httpClient, _appHost, _serverConfigurationManager, "start", "syncplayed", cancellationToken).ConfigureAwait(false);

            var config = Plugin.Instance.Configuration;

            BlurNItems items = new BlurNItems();

            string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

            if (_fileSystem.FileExists(dataPath))
                items.List = _json.DeserializeFromFile<List<BlurNItem>>(dataPath);

            var users = _userManager.Users.ToList();

            Plugin.DebugLogger($"Syncing played status of {users.Count} users to library.");

            for (int u = 0; u < users.Count; u++)
            {
                User user = users[u];
                Dictionary<string, BaseItem> libDict = Library.BuildLibraryDictionary(cancellationToken, _libraryManager, new InternalItemsQuery()
                {
                    HasAnyProviderId = new[] { "Imdb" },
                    User = user,
                    IsPlayed = false,
                    SourceTypes = new SourceType[] { SourceType.Library }
                });

                Plugin.DebugLogger($"User {user.Name} has {libDict.Count} unplayed movies in library.");

                for (int i = 0; i < items.List.Count; i++)
                {
                    BlurNItem blurNItem = items.List[i];
                    BaseItem libraryItem;
                    if (libDict.TryGetValue(blurNItem.ImdbId, out libraryItem))
                    {
                        UserItemData uid = _userDataManager.GetAllUserData(user.InternalId).FirstOrDefault(aud => aud.Key == $"{config.ChannelRefreshCount}-{blurNItem.ImdbId}");
                        if (uid != default(UserItemData))
                        {
                            if (uid.Played)
                            {
                                libraryItem.MarkPlayed(user, uid.LastPlayedDate, true);
                                Plugin.DebugLogger($"Marked {blurNItem.Title} as watched in movie library.");
                            }
                        }
                    }

                    progress.Report(((u + ((i + 1) / items.List.Count)) / users.Count) * 100);
                }
            }

            await Tracking.Track(_httpClient, _appHost, _serverConfigurationManager, "end", "syncplayed", cancellationToken).ConfigureAwait(false);

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
