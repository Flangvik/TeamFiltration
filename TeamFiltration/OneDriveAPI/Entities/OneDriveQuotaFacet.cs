using KoenZomers.OneDrive.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// The Quota facet groups storage space quota-related information on OneDrive into a single structure
    /// </summary>
    public class OneDriveQuotaFacet
    {
        /// <summary>
        /// Total allowed storage space, in bytes
        /// </summary>
        [JsonProperty("total")]
        public long Total { get; set; }

        /// <summary>
        /// Total space used, in bytes
        /// </summary>
        [JsonProperty("used")]
        public long Used { get; set; }

        /// <summary>
        /// Total space remaining before reaching the quota limit, in bytes
        /// </summary>
        [JsonProperty("remaining")]
        public long Remaining { get; set; }

        /// <summary>
        /// Total space consumed by files in the recycle bin, in bytes
        /// </summary>
        [JsonProperty("deleted")]
        public long Deleted { get; set; }

        /// <summary>
        /// Enumeration value that indicates the state of the storage space
        /// </summary>
        [JsonProperty("state"), JsonConverter(typeof(StringEnumConverter))]
        public OneDriveQuotaState State { get; set; }
    }
}
