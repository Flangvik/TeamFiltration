using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveSpecialFolderFacet
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
