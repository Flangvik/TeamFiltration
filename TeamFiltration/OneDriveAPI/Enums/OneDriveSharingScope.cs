using System.Runtime.Serialization;

namespace KoenZomers.OneDrive.Api.Enums
{
    /// <summary>
    /// Scopes with which an item can be shared in OneDrive
    /// </summary>
    public enum OneDriveSharingScope
    {
        /// <summary>
        /// Creates a link to the DriveItem accessible to anyone with the link. Anonymous links may be disabled by an administrator.
        /// </summary>
        [EnumMember(Value = "anonymous")]
        Anonymous,

        /// <summary>
        /// Creates a link to the DriveItem accessible to anyone within the user's organization. Organization link scope is not available for OneDrive personal.
        /// </summary>
        [EnumMember(Value = "organization")]
        Organization
    }
}
