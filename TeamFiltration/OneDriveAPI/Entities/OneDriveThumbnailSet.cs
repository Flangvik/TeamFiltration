using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveThumbnailSet : OneDriveItemBase
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("small", NullValueHandling=NullValueHandling.Ignore)]
        public OneDriveThumbnail Small { get; set; }

        [JsonProperty("medium", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveThumbnail Medium { get; set; }

        [JsonProperty("large", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveThumbnail Large { get; set; }
    }
}
