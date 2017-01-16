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
        public Boolean EnableNewReleaseNotification { get; set; }
        public Boolean EnableDebugLogging { get; set; }
        public OMDBList Items { get; set; }

        public PluginConfiguration()
        {
            MinimumIMDBRating = 7.0m;
            MinimumIMDBVotes = 1000;
            Age = 365;
            LastPublishDate = DateTime.MinValue;
            AddItemsAlreadyInLibrary = true;
            EnableNewReleaseNotification = true;
            EnableDebugLogging = false;
            Items = new OMDBList();
        }
    }
}
