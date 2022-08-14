using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Response to a specific user getting access after a new permission request on a OneDrive item
    /// </summary>
    public class OneDrivePermissionResponseGrant : OneDriveItemBase
    {
        /// <summary>
        /// The user that has been granted access
        /// </summary>
        [JsonProperty("user")]
        public OneDriveUserProfile User { get; set; }
    }
}
