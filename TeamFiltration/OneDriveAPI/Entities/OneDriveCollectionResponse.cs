using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveCollectionResponse<T> : OneDriveItemBase
    {
        [JsonProperty("value")]
        public T[] Collection { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string NextLink { get; set; }
    }
}
