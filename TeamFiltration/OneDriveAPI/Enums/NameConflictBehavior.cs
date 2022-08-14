using System.Runtime.Serialization;

namespace KoenZomers.OneDrive.Api.Enums
{
    public enum NameConflictBehavior
    {
        [EnumMember(Value = "fail")]
        Fail,

        [EnumMember(Value = "replace")]
        Replace,

        [EnumMember(Value = "rename")]
        Rename
    }
}
