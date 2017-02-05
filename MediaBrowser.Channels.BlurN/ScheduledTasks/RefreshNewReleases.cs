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
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Channels.BlurN.ScheduledTasks
{
    class RefreshNewReleases : IScheduledTask
    {
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

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

        public virtual OMDB ParseOMDB(string url, DateTime bluRayReleaseDate)
        {
            try
            {
                XDocument doc = XDocument.Load(url);
                var entry = doc.Root.Element("movie");

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
                    Year = Convert.ToInt32(entry.Attribute("year").Value),
                    ImdbRating = Convert.ToDecimal(entry.Attribute("imdbRating").Value),
                    ImdbVotes = Convert.ToInt32(entry.Attribute("imdbVotes").Value.Replace(",", "")),
                    Released = ParseDate(entry.Attribute("released").Value)
                };
            }
            catch
            {
                return new OMDB();
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

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string feedURL = "http://www.blu-ray.com/rss/newreleasesfeed.xml";

            string result;
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                request.RequestUri = new Uri(feedURL);
                using (HttpResponseMessage response = await client.SendAsync(request))
                using (HttpContent content = response.Content)
                {
                    // ... Read the string.
                    result = await content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            XDocument doc = XDocument.Parse(result);

            var entries = from item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item")
                          select new Item
                          {
                              FeedType = FeedType.RSS,
                              Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                              Link = item.Elements().First(i => i.Name.LocalName == "link").Value,
                              PublishDate = ParseDate(item.Elements().First(i => i.Name.LocalName == "pubDate").Value),
                              Title = item.Elements().First(i => i.Name.LocalName == "title").Value.Replace(" 4K (Blu-ray)", "").Replace(" (Blu-ray)", "")
                          };

            IList<Item> items = entries.ToList();

            var config = Plugin.Instance.Configuration;
            bool debug = config.EnableDebugLogging;
            DateTime lastPublishDate = config.LastPublishDate;
            DateTime minAge = DateTime.Today.AddDays(0 - config.Age);
            DateTime newPublishDate = items[0].PublishDate;

            IEnumerable<BaseItem> library;
            Dictionary<string, BaseItem> libDict = new Dictionary<string, BaseItem>();

            if (!config.AddItemsAlreadyInLibrary)
            {
                library = _libraryManager.GetItemList(new InternalItemsQuery() { HasImdbId = true, SourceTypes = new SourceType[] { SourceType.Library } });

                cancellationToken.ThrowIfCancellationRequested();

                foreach (BaseItem libItem in library)
                {
                    string libIMDbId = libItem.GetProviderId(MetadataProviders.Imdb);
                    if (!libDict.ContainsKey(libIMDbId))
                        libDict.Add(libIMDbId, libItem);
                }

            }

            cancellationToken.ThrowIfCancellationRequested();

            var insertList = new OMDBList();

            var finalItems = items.Where(i => i.PublishDate > lastPublishDate).GroupBy(x => new { x.Title, x.PublishDate }).Select(g => g.First()).Reverse().ToList();

            for (int i = 0; i < finalItems.Count(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(100d * (Convert.ToDouble(i + 1) / Convert.ToDouble(finalItems.Count())));
                Item item = finalItems[i];
                int year = 0;
                Regex rgx = new Regex(@"\| (\d{4}) \|", RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(item.Content);
                if (matches.Count > 0)
                {
                    Match match = matches[matches.Count - 1];
                    Group group = match.Groups[match.Groups.Count - 1];
                    year = Convert.ToInt32(group.Value);
                }

                string url;
                if (year > 0)
                    url = "http://www.omdbapi.com/?t=" + WebUtility.UrlEncode(item.Title) + "&y=" + year.ToString() + "&plot=short&r=xml";
                else
                    url = "http://www.omdbapi.com/?t=" + WebUtility.UrlEncode(item.Title) + "&plot=short&r=xml";

                OMDB omdb = ParseOMDB(url, item.PublishDate);
                if (string.IsNullOrEmpty(omdb.ImdbId) && item.Title.EndsWith(" 3D") && year > 0)
                {
                    url = "http://www.omdbapi.com/?t=" + WebUtility.UrlEncode(item.Title.Remove(item.Title.Length - 3)) + "&y=" + year.ToString() + "&plot=short&r=xml";
                    omdb = ParseOMDB(url, item.PublishDate);
                }

                if (!string.IsNullOrEmpty(omdb.ImdbId) && !config.AddItemsAlreadyInLibrary && libDict.ContainsKey(omdb.ImdbId))
                {
                    if (debug)
                        Plugin.Logger.Debug(omdb.ImdbId + " is already in the library, skipped.");
                }
                else if (omdb.Type == "movie" && omdb.ImdbRating >= config.MinimumIMDBRating && omdb.ImdbVotes >= config.MinimumIMDBVotes && omdb.Released > minAge)
                {
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
                }
            }

            if (config.Items.List.Count > 0)
                insertList.List.AddRange(config.Items.List);

            string dataPath = Path.Combine(_appPaths.PluginConfigurationsPath, "MediaBrowser.Channels.BlurN.Data.json");

            if (_fileSystem.FileExists(dataPath))
            {
                var existingData = _json.DeserializeFromFile<List<OMDB>>(dataPath);

                if (existingData != null)
                    insertList.List.AddRange(existingData);
            }

            insertList.List = insertList.List.OrderByDescending(i => i.BluRayReleaseDate).ThenByDescending(i => i.ImdbRating).ThenByDescending(i => i.ImdbVotes).ThenByDescending(i => i.Metascore).ThenBy(i => i.Title).ToList();

            config.LastPublishDate = newPublishDate;
            config.Items = new OMDBList();
            Plugin.Instance.SaveConfiguration();

            if (debug)
                Plugin.Logger.Debug("BlurN configuration saved");

            if (debug)
                Plugin.Logger.Debug("BlurN MediaBrowser.Channels.BlurN.Data.json path is " + dataPath);

            _json.SerializeToFile(insertList.List, dataPath);

            if (debug)
                Plugin.Logger.Debug("BlurN data json file saved");

            progress.Report(100);
            return;
        }

        private void AddVideo(OMDB omdb)
        {
            var items = Plugin.Instance.Configuration.Items;
            items.List.Add(omdb);
            Plugin.Instance.SaveConfiguration();
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
