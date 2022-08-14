using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveThumbnail : OneDriveItemBase
    {
        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
