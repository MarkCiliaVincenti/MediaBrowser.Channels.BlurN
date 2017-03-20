using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Channels.BlurN.Helpers;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Channels.BlurN.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public decimal MinimumIMDBRating { get; set; }
        public int MinimumIMDBVotes { get; set; }
        public int Age { get; set; }
        public DateTime LastPublishDate { get; set; }
        public Boolean AddItemsAlreadyInLibrary { get; set; }
        public Boolean HidePlayedMovies { get; set; }
        public Boolean EnableDebugLogging { get; set; }
        public int ChannelRefreshCount { get; set; }
        public string InstallationID { get; set; }

        public PluginConfiguration()
        {
            ChannelRefreshCount = 1;
            MinimumIMDBRating = 6.8m;
            MinimumIMDBVotes = 1000;
            Age = 730;
            LastPublishDate = DateTime.MinValue;
            AddItemsAlreadyInLibrary = true;
            HidePlayedMovies = true;
            EnableDebugLogging = false;
            InstallationID = Guid.NewGuid().ToString();
        }
    }
}
