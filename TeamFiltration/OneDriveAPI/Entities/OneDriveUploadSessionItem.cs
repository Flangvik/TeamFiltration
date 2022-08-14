using KoenZomers.OneDrive.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KoenZomers.OneDrive.Api.Entities
{
    internal class OneDriveUploadSessionItem
    {
        [JsonProperty("@name.conflictBehavior", DefaultValueHandling = DefaultValueHandling.Populate), JsonConverter(typeof(StringEnumConverter))]
        public NameConflictBehavior FilenameConflictBehavior { get; set; }

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Filename { get; set; }
    }
}
