using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    internal class GraphApiUploadSessionItemContainer : OneDriveItemBase
    {
        [JsonProperty("item", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public GraphApiUploadSessionItem Item { get; set; }
    }
}
