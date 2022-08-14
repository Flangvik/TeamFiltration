using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveParentItemReference : OneDriveItemBase
    {
        [JsonProperty("parentReference")]
        public OneDriveItemReference ParentReference { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
