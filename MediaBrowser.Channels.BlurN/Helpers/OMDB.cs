using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    /// <summary>
    /// Represents an OMDB entry.
    /// </summary>
    [XmlType("OMDB")]
    public class OMDB
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

        [XmlElement("Genre")]
        public string Genre { get; set; }

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

        [XmlElement("Type")]
        public string Type { get; set; }

        public OMDB()
        {
        }
    }

    [XmlRoot("BlurNOMDBList")]
    [XmlInclude(typeof(OMDB))]
    public class OMDBList
    {
        [XmlArray("BlurNOMDBArray")]
        [XmlArrayItem("BlurNOMDBObject")]
        public List<OMDB> List = new List<OMDB>();
    }
}
