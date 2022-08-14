using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveAudioFacet
    {
        [JsonProperty("album", NullValueHandling=NullValueHandling.Ignore)]
        public string Album { get; set; }

        [JsonProperty("albumArtist", NullValueHandling = NullValueHandling.Ignore)]
        public string AlbumArtist { get; set; }

        [JsonProperty("artist", NullValueHandling = NullValueHandling.Ignore)]
        public string Artist { get; set; }

        [JsonProperty("bitrate", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public int BitRate { get; set; }

        [JsonProperty("copyright", NullValueHandling = NullValueHandling.Ignore)]
        public string Copyright { get; set; }

        [JsonProperty("disc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Disc { get; set; }

        [JsonProperty("discCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int DiscCount { get; set; }

        [JsonProperty("duration", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Duration { get; set; }

        [JsonProperty("genre", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Genre { get; set; }

        [JsonProperty("hasDrm", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasDrm { get; set; }

        [JsonProperty("isVariableBitrate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsVariableBitRate { get; set; }

        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("track", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Track { get; set; }

        [JsonProperty("trackCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int TrackCount { get; set; }

        [JsonProperty("year", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Year { get; set; }
    }
}
