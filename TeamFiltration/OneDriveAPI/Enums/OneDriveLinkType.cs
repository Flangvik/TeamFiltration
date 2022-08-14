using System.Runtime.Serialization;

namespace KoenZomers.OneDrive.Api.Enums
{
    /// <summary>
    /// Type of rights assignable to a OneDrive item
    /// </summary>
    public enum OneDriveLinkType
    {
        /// <summary>
        /// Read-only
        /// </summary>
        [EnumMember(Value = "view")]
        View,

        /// <summary>
        /// Read-write
        /// </summary>
        [EnumMember(Value = "edit")]
        Edit
    }
}
