using KoenZomers.OneDrive.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KoenZomers.OneDrive.Api.Entities
{
    internal class GraphApiUploadSessionItem
    {
        [JsonProperty("@microsoft.graph.conflictBehavior", DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(StringEnumConverter))]
        public NameConflictBehavior FilenameConflictBehavior { get; set; }

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Filename { get; set; }
    }
}
