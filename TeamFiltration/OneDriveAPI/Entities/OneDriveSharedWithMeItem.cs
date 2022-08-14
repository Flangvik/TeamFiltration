using System;
using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Entity for a shared with me item
    /// </summary>
    public class OneDriveSharedWithMeItem
    {
        /// <summary>
        /// Date and time at which the item was created
        /// </summary>
        [JsonProperty("createdDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Unique OneDrive item ID
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Date and time at which this item was last modified
        /// </summary>
        [JsonProperty("lastModifiedDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Details of this item
        /// </summary>
        [JsonProperty("remoteItem", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OneDriveRemoteItem RemoteItem { get; set; }

        /// <summary>
        /// Size of this item
        /// </summary>
        [JsonProperty("size", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Size { get; set; }
    }
}
