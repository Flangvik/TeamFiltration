using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    internal class OneDriveCreateFolder : OneDriveItemBase
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("folder")]
        public object Folder { get; set; }
    }
}
