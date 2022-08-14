using System.Runtime.Serialization;

namespace KoenZomers.OneDrive.Api.Enums
{
    /// <summary>
    /// Enumeration value that indicates the state of the storage space
    /// </summary>
    public enum OneDriveQuotaState
    {
        [EnumMember(Value = "normal")]
        Normal,

        [EnumMember(Value = "nearing")]
        Nearing,

        [EnumMember(Value = "critical")]
        Critical,

        [EnumMember(Value = "exceeded")]
        Exceeded,

        [EnumMember(Value = "Ok")]
        Ok
    }
}
