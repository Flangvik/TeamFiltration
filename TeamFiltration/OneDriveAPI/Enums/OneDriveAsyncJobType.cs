using System.Runtime.Serialization;

namespace KoenZomers.OneDrive.Api.Enums
{
    public enum OneDriveAsyncJobType
    {
        [EnumMember(Value = "DownloadUrl")]
        DownloadUrl,
        [EnumMember(Value = "CopyItem")]
        CopyItem
    }
}
