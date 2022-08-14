using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Invitation for a specific user getting access after a new permission request on a OneDrive item
    /// </summary>
    public class OneDrivePermissionResponseInvitation : OneDriveItemBase
    {
        /// <summary>
        /// E-mail address of the user being invited
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// Boolean indicating if the user is required to sign in before the OneDrive item can be accessed
        /// </summary>
        [JsonProperty("signInRequired")]
        public bool SignInRequired { get; set; }
    }
}
