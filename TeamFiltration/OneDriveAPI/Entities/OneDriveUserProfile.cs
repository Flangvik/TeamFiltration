using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Represents an user
    /// </summary>
    public class OneDriveUserProfile : OneDriveItemBase
    {
        /// <summary>
        /// Unique identifier of the user
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Friendly name of the user
        /// </summary>
        [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// E-mail address of the user
        /// </summary>
        [JsonProperty("emailAddress", NullValueHandling = NullValueHandling.Ignore)]
        public string EmailAddress { get; set; }

        /// <summary>
        /// Username of the user
        /// </summary>
        [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
        public string Username { get; set; }

        /// <summary>
        /// Organization the user belongs to
        /// </summary>
        [JsonProperty("organization", NullValueHandling = NullValueHandling.Ignore)]
        public string Organization { get; set; }
    }
}
