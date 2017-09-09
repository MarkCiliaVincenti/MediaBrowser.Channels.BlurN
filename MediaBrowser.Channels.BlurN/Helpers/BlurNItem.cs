using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    public class FailedBlurNItem
    {
        public string Title { get; set; }

        public int Year { get; set; }

        public FailedBlurNItem()
        {
        }
    }

    /// <summary>
    /// Represents a BlurNItem entry.
    /// </summary>
    public class BlurNItem
    {
        public DateTime BluRayReleaseDate { get; set; }

        public string Title { get; set; }

        public int Year { get; set; }

        public string Rated { get; set; }

        public DateTime Released { get; set; }

        public string Runtime { get; set; }

        private long? _runTimeTicks;
        public long? RuntimeTicks
        {
            get
            {
                if (_runTimeTicks.HasValue)
                    return _runTimeTicks;
                RuntimeTicks = ConvertRuntimeToTicks(Runtime);
                return _runTimeTicks;
            }
            set
            {
                _runTimeTicks = value;
            }
        }

        public static long? ConvertRuntimeToTicks(string runTime)
        {
            if (string.IsNullOrEmpty(runTime) || (runTime == "N/A"))
                return null;
            int mins;
            if (Int32.TryParse(runTime.Split(' ')[0], out mins))
                return TimeSpan.FromMinutes(mins).Ticks;
            return null;
        }

        public string Genre { get; set; }

        public string FirstGenre
        {
            get { return Genre.Split(',')[0]; }
        }

        public string Director { get; set; }

        public string Writer { get; set; }

        public string Actors { get; set; }

        public string Plot { get; set; }

        public string Language { get; set; }

        public string Country { get; set; }

        public string Awards { get; set; }

        public string Poster { get; set; }

        public string Metascore { get; set; }

        public decimal ImdbRating { get; set; }

        public int ImdbVotes { get; set; }

        public string ImdbId { get; set; }

        public string ImdbUrl
        {
            get
            {
                return $"http://www.imdb.com/title/{ImdbId}/";
            }
        }

        public string Type { get; set; }

        public int? TmdbId { get; set; }

        public BaseItem LibraryItem { get; set; }

        public BlurNItem()
        {
        }
    }

    public class BlurNItems
    {
        public List<BlurNItem> List = new List<BlurNItem>();
    }

    public class FailedBlurNList
    {
        public List<FailedBlurNItem> List = new List<FailedBlurNItem>();
    }

}
