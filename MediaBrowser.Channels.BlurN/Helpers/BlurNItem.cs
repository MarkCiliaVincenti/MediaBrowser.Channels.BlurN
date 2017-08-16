using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    [XmlType("FailedBlurNItem")]
    public class FailedBlurNItem
    {
        [XmlElement("Title")]
        public string Title { get; set; }

        [XmlElement("Year")]
        public int Year { get; set; }

        public FailedBlurNItem()
        {
        }
    }

    /// <summary>
    /// Represents a BlurNItem entry.
    /// </summary>
    [XmlType("BlurNItem")]
    public class BlurNItem
    {
        [XmlElement("BluRayReleaseDate")]
        public DateTime BluRayReleaseDate { get; set; }

        [XmlElement("Title")]
        public string Title { get; set; }

        [XmlElement("Year")]
        public int Year { get; set; }

        [XmlElement("Rated")]
        public string Rated { get; set; }

        [XmlElement("Released")]
        public DateTime Released { get; set; }

        [XmlElement("Runtime")]
        public string Runtime { get; set; }

        [XmlElement("RuntimeTicks")]
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

        [XmlElement("Genre")]
        public string Genre { get; set; }

        public string FirstGenre
        {
            get { return Genre.Split(',')[0]; }
        }

        [XmlElement("Director")]
        public string Director { get; set; }

        [XmlElement("Writer")]
        public string Writer { get; set; }

        [XmlElement("Actors")]
        public string Actors { get; set; }

        [XmlElement("Plot")]
        public string Plot { get; set; }

        [XmlElement("Language")]
        public string Language { get; set; }

        [XmlElement("Country")]
        public string Country { get; set; }

        [XmlElement("Awards")]
        public string Awards { get; set; }

        [XmlElement("Poster")]
        public string Poster { get; set; }

        [XmlElement("Metascore")]
        public string Metascore { get; set; }

        [XmlElement("ImdbRating")]
        public decimal ImdbRating { get; set; }

        [XmlElement("ImdbVotes")]
        public int ImdbVotes { get; set; }

        [XmlElement("ImdbId")]
        public string ImdbId { get; set; }

        public string ImdbUrl
        {
            get
            {
                return $"http://www.imdb.com/title/{ImdbId}/";
            }
        }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("TmdbId")]
        public int? TmdbId { get; set; }

        public BaseItem LibraryItem { get; set; }

        public BlurNItem()
        {
        }
    }

    [XmlRoot("BlurNItems")]
    [XmlInclude(typeof(BlurNItem))]
    public class BlurNItems
    {
        [XmlArray("BlurNItemArray")]
        [XmlArrayItem("BlurNItemObject")]
        public List<BlurNItem> List = new List<BlurNItem>();
    }

    [XmlRoot("FailedBlurNList")]
    [XmlInclude(typeof(FailedBlurNItem))]
    public class FailedBlurNList
    {
        [XmlArray("FailedBlurNItemArray")]
        [XmlArrayItem("FailedBlurNItemObject")]
        public List<FailedBlurNItem> List = new List<FailedBlurNItem>();
    }

}
