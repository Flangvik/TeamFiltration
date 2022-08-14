using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// The DriveRecipient resource represents a person, group, or other recipient to share with using the invite action
    /// </summary>
    public class OneDriveDriveRecipient : OneDriveItemBase
    {
        /// <summary>
        /// The email address for the recipient, if the recipient has an associated email address
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// The alias of the domain object, for cases where an email address is unavailable (e.g. security groups)
        /// </summary>
        [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
        public string Alias { get; set; }

        /// <summary>
        /// The unique identifier for the recipient in the directory
        /// </summary>
        [JsonProperty("objectid", NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }
    }
}
