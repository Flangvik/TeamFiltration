using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// The Identity type represents an identity of an actor. For example, and actor can be a user, device, or application.
    /// </summary>
    public class OneDriveIdentity : OneDriveItemBase
    {
        /// <summary>
        ///  Unique identifier for the identity
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The identity's display name. Note that this may not always be available or up to date. For example, if a user changes their display name, OneDrive may show the new value in a future response, but the items associated with the user won't show up as having changed in view.changes
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
