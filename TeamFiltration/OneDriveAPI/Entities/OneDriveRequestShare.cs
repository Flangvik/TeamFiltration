using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Message to request sharing of an item
    /// </summary>
    internal class OneDriveRequestShare : OneDriveItemBase
    {
        /// <summary>
        /// Type of sharing to request
        /// </summary>
        [JsonProperty("type")]
        public Enums.OneDriveLinkType SharingType { get; set; }

        /// <summary>
        /// Scope of the access to the shared item
        /// </summary>
        [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
        public Enums.OneDriveSharingScope? Scope { get; set; }
    }
}
