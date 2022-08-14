using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveFolderFacet
    {
        [JsonProperty("childCount")]
        public long ChildCount { get; set; }
    }
}
