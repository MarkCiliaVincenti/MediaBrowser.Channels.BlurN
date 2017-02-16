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
using System.Text.RegularExpressions;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Providers;
using System.Net.Http;
using System.Reflection;

namespace MediaBrowser.Channels.BlurN.ScheduledTasks
{
    class RefreshNewReleases : IScheduledTask
    {
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

        private const string bluRayReleaseUri = "http://www.blu-ray.com/rss/newreleasesfeed.xml";
        private const string baseOmdbApiUri = "http://www.omdbapi.com";

        public RefreshNewReleases(IJsonSerializer json, IApplicationPaths appPaths, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
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
                return "Checks for new Blu-Ray releases that match the filters in the settings and adds them to the BlurN channel.";
            }
        }

        public string Key
        {
            get
            {
                return "BlurNRefreshNewReleases";
            }
        }

        public string Name
        {
            get
            {
                return "Refresh new releases";
            }
        }

        public int Order
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public async virtual Task<OMDB> ParseOMDB(string url, DateTime bluRayReleaseDate)
        {
            string result = "";
            try
            {
                result = await HTTP.GetPage(url).ConfigureAwait(false);
                XDocument doc = XDocument.Parse(result);
                XElement root = doc.Root;
                if (root.Elements().First().Name.ToString() == "movie")
                {
                    var entry = doc.Root.Element("movie");

                    int year = 0;
                    Int32.TryParse(entry.Attribute("year").Value, out year);

                    decimal imdbRating = 0;
                    decimal.TryParse(entry.Attribute("imdbRating").Value, out imdbRating);

                    int imdbVotes = 0;
                    Int32.TryParse(entry.Attribute("imdbVotes").Value.Replace(",", ""), out imdbVotes);

                    DateTime released = DateTime.MinValue;
                    if (entry.Attribute("released").Value != "N/A")
                        released = ParseDate(entry.Attribute("released").Value);

                    return new OMDB()
                    {
                        BluRayReleaseDate = bluRayReleaseDate,
                        Actors = entry.Attribute("actors").Value,
                        Awards = entry.Attribute("awards").Value,
                        Country = entry.Attribute("country").Value,
                        Director = entry.Attribute("director").Value,
                        Genre = entry.Attribute("genre").Value,
                        ImdbId = entry.Attribute("imdbID").Value,
                        Language = entry.Attribute("language").Value,
                        Metascore = entry.Attribute("metascore").Value,
                        Plot = entry.Attribute("plot").Value,
                        Poster = entry.Attribute("poster").Value,
                        Rated = entry.Attribute("rated").Value,
                        Runtime = entry.Attribute("runtime").Value,
                        Type = entry.Attribute("type").Value,
                        Writer = entry.Attribute("writer").Value,
                        Title = entry.Attribute("title").Value,
                        Year = year,
                        ImdbRating = imdbRating,
                        ImdbVotes = imdbVotes,
                        Released = released
                    };
                }
                else if (Plugin.Instance.Configuration.EnableDebugLogging)
                {
                    Plugin.Logger.Debug("[BlurN] Received an error from " + url + " - " + root.Elements().First().Value);
                }
                return new OMDB();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private DateTime ParseDate(string date)
        {
            DateTime result;
            if (DateTime.TryParse(date, out result))
                return result;
            else
                return DateTime.MinValue;
        }

        private async Task Track()
        {
            if (string.IsNullOrEmpty(Plugin.Instance.Configuration.InstallationID))
            {
                Plugin.Instance.Configuration.InstallationID = Guid.NewGuid().ToString();
                Plugin.Instance.SaveConfiguration();
            }

            try
            {
                using (var client = new HttpClient())
                {
                    string version = typeof(RefreshNewReleases).GetTypeInfo().Assembly.GetName().Version.ToString();
                    var values = new Dictionary<string, string>
                    {
                        { "v", "1" },
                        { "t", "event" },
                        { "tid", "UA-92060336-1" },
                        { "cid", Plugin.Instance.Configuration.InstallationID },
                        { "ec", "refresh" },
                        { "ea", version },
                        { "el", Plugin.Instance.Configuration.ChannelRefreshCount.ToString() },
                        { "an", "BlurN" },
                        { "aid", "MediaBrowser.Channels.BlurN" },
                        { "av", version },
                        { "ds", "embyserver" }
                    };

                    var content = new FormUrlEncodedContent(values);
                    var response = await client.PostAsync("https://www.google-analytics.com/collect", content);
                    var responseString = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Instance.Configuration.EnableDebugLogging)
                    Plugin.Logger.Debug("[BlurN] Failed to track usage with GA: " + ex.Message);
            }
        }


        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = (await GetBluRayReleaseItems(cancellationToken).ConfigureAwait(false)).List;

            var config = await BlurNTasks.CheckIfResetDatabaseRequested(cancellationToken, _json, _appPaths, _fileSystem).ConfigureAwait(false);

            string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

            if (config.ChannelRefreshCount < 3 && _fileSystem.FileExists(dataPath))
            {
                // Convert posters from w640 to original
                var existingData = _json.DeserializeFromFile<List<OMDB>>(dataPath);

                if (existingData != null)
                {
                    foreach (OMDB omdb in existingData.Where(o => o.TmdbId.HasValue))
                        omdb.Poster = omdb.Poster.Replace("/w640/", "/original/");

                    _json.SerializeToFile(existingData, dataPath);
                }

                config.ChannelRefreshCount = 3;
                Plugin.Instance.SaveConfiguration();
            }


            bool debug = config.EnableDebugLogging;

            if (debug)
                Plugin.Logger.Debug("[BlurN] Found " + items.Count + " items in feed");

            DateTime lastPublishDate = config.LastPublishDate;
            DateTime minAge = DateTime.Today.AddDays(0 - config.Age);
            DateTime newPublishDate = items[0].PublishDate;
            Dictionary<string, BaseItem> libDict = (config.AddItemsAlreadyInLibrary) ? Library.BuildLibraryDictionary(cancellationToken, _libraryManager, new InternalItemsQuery() { HasImdbId = true, SourceTypes = new SourceType[] { SourceType.Library } }) : new Dictionary<string, BaseItem>();

            cancellationToken.ThrowIfCancellationRequested();

            var insertList = new OMDBList();
            var failedList = new FailedOMDBList();

            var finalItems = items.Where(i => i.PublishDate > lastPublishDate).GroupBy(x => new { x.Title, x.PublishDate }).Select(g => g.First()).Reverse().ToList();

            string failedDataPath = AddPreviouslyFailedItemsToFinalItems(finalItems);

            if (debug)
                Plugin.Logger.Debug("[BlurN] Checking " + finalItems.Count + " new items");

            for (int i = 0; i < finalItems.Count(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(100d * (Convert.ToDouble(i + 1) / Convert.ToDouble(finalItems.Count())));

                Item item = finalItems[i];
                int year = 0;

                if (item.Link == "Failed")  // previously failed item
                    year = Convert.ToInt32(item.Content);
                else // new item
                {
                    Regex rgx = new Regex(@"\| (\d{4}) \|", RegexOptions.IgnoreCase);
                    MatchCollection matches = rgx.Matches(item.Content);
                    if (matches.Count > 0)
                    {
                        Match match = matches[matches.Count - 1];
                        Group group = match.Groups[match.Groups.Count - 1];
                        year = Convert.ToInt32(group.Value);
                    }
                }

                string url;
                if (year > 0)
                    url = baseOmdbApiUri + "/?t=" + WebUtility.UrlEncode(item.Title) + "&y=" + year.ToString() + "&plot=short&r=xml";
                else
                    url = baseOmdbApiUri + "/?t=" + WebUtility.UrlEncode(item.Title) + "&plot=short&r=xml";

                OMDB omdb = await ParseOMDB(url, item.PublishDate).ConfigureAwait(false);
                if (omdb != null && string.IsNullOrEmpty(omdb.ImdbId) && (item.Title.EndsWith(" 3D") || item.Title.EndsWith(" 4K")) && year > 0)
                {
                    url = baseOmdbApiUri + "/?t=" + WebUtility.UrlEncode(item.Title.Remove(item.Title.Length - 3)) + "&y=" + year.ToString() + "&plot=short&r=xml";
                    omdb = await ParseOMDB(url, item.PublishDate).ConfigureAwait(false);
                }
                if (omdb == null)
                    failedList.List.Add(new FailedOMDB() { Title = item.Title, Year = year });

                else if (!string.IsNullOrEmpty(omdb.ImdbId) && !config.AddItemsAlreadyInLibrary && libDict.ContainsKey(omdb.ImdbId))
                {
                    if (debug)
                        Plugin.Logger.Debug("[BlurN] " + omdb.ImdbId + " is already in the library, skipped.");
                }
                else if (omdb.Type == "movie" && omdb.ImdbRating >= config.MinimumIMDBRating && omdb.ImdbVotes >= config.MinimumIMDBVotes && omdb.Released > minAge)
                {
                    await UpdateContentWithTmdbData(cancellationToken, omdb).ConfigureAwait(false);

                    insertList.List.Add(omdb);

                    if (config.EnableNewReleaseNotification)
                    {
                        var variables = new Dictionary<string, string>();
                        variables.Add("Title", omdb.Title);
                        variables.Add("Year", omdb.Year.ToString());
                        variables.Add("IMDbRating", omdb.ImdbRating.ToString());
                        await Plugin.NotificationManager.SendNotification(new NotificationRequest()
                        {
                            Variables = variables,
                            Date = DateTime.Now,
                            Level = NotificationLevel.Normal,
                            SendToUserMode = SendToUserType.All,
                            NotificationType = "BlurNNewRelease"
                        }, cancellationToken).ConfigureAwait(false);
                    }

                    if (debug)
                        Plugin.Logger.Debug("[BlurN] Adding " + omdb.Title + " to the BlurN channel.");
                }
            }

            if (config.Items.List.Count > 0)
                insertList.List.AddRange(config.Items.List);

            if (_fileSystem.FileExists(dataPath))
            {
                var existingData = _json.DeserializeFromFile<List<OMDB>>(dataPath);

                if (existingData != null)
                {
                    foreach (OMDB omdb in existingData.Where(o => !o.TmdbId.HasValue))
                        await UpdateContentWithTmdbData(cancellationToken, omdb).ConfigureAwait(false);

                    insertList.List.AddRange(existingData);
                }
            }

            insertList.List = insertList.List.OrderByDescending(i => i.BluRayReleaseDate).ThenByDescending(i => i.ImdbRating).ThenByDescending(i => i.ImdbVotes).ThenByDescending(i => i.Metascore).ThenBy(i => i.Title).ToList();

            config.LastPublishDate = newPublishDate;
            config.Items = new OMDBList();
            Plugin.Instance.SaveConfiguration();

            if (debug)
                Plugin.Logger.Debug("[BlurN] Configuration saved. MediaBrowser.Channels.BlurN.Data.json path is " + dataPath);

            _json.SerializeToFile(insertList.List, dataPath);
            _json.SerializeToFile(failedList.List, failedDataPath);

            if (debug)
                Plugin.Logger.Debug("[BlurN] json files saved");

            await Track().ConfigureAwait(false);

            progress.Report(100);
            return;
        }

        private async Task UpdateContentWithTmdbData(CancellationToken cancellationToken, OMDB omdb)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string tmdbContent = await HTTP.GetPage("https://api.themoviedb.org/3/find/" + omdb.ImdbId + "?api_key=3e97b8d1c00a0f2fe72054febe695276&external_source=imdb_id").ConfigureAwait(false);
                var tmdb = _json.DeserializeFromString<TmdbMovieFindResult>(tmdbContent);
                TmdbMovieSearchResult tmdbMovie = tmdb.movie_results.First();
                omdb.Poster = "https://image.tmdb.org/t/p/original" + tmdbMovie.poster_path;
                omdb.TmdbId = tmdbMovie.id;
            }
            catch
            { }
        }

        private string AddPreviouslyFailedItemsToFinalItems(List<Item> finalItems)
        {
            string failedDataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Failed.json");

            if (_fileSystem.FileExists(failedDataPath))
            {
                var existingFailedList = _json.DeserializeFromFile<List<FailedOMDB>>(failedDataPath);

                if (existingFailedList != null)
                {
                    foreach (FailedOMDB failedItem in existingFailedList)
                    {
                        finalItems.Add(new Item() { Link = "Failed", Content = failedItem.Year.ToString(), Title = failedItem.Title });
                    }
                }
            }

            return failedDataPath;
        }

        private async Task<ItemList> GetBluRayReleaseItems(CancellationToken cancellationToken)
        {
            string bluRayReleaseContent = await HTTP.GetPage(bluRayReleaseUri).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            XDocument doc = XDocument.Parse(bluRayReleaseContent);

            var entries = from item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item")
                          select new Item
                          {
                              FeedType = FeedType.RSS,
                              Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                              Link = item.Elements().First(i => i.Name.LocalName == "link").Value,
                              PublishDate = ParseDate(item.Elements().First(i => i.Name.LocalName == "pubDate").Value),
                              Title = item.Elements().First(i => i.Name.LocalName == "title").Value.Replace(" 4K (Blu-ray)", "").Replace(" (Blu-ray)", "")
                          };

            ItemList items = new ItemList() { List = entries.ToList() };
            return items;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Until we can vary these default triggers per server and MBT, we need something that makes sense for both
            return new[] { 
            
                // At startup
                //new TaskTriggerInfo {Type = TaskTriggerInfo.TriggerStartup},

                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(4).Ticks}
            };
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool Supports(IHasImages item)
        {
            throw new NotImplementedException();
        }
    }
}
