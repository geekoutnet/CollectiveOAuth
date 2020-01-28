using System.ComponentModel;

namespace Come.CollectiveOAuth.Enums
{
    public enum AuthUserGender
    {
        [Description("男")]
        MALE=1,
        [Description("女")]
        FEMALE = 0,
        [Description("未知")]
        UNKNOWN = -1
    }
}