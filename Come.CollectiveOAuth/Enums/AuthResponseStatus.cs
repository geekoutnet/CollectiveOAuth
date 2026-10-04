using System.ComponentModel;

namespace Come.CollectiveOAuth.Enums
{
    public enum AuthResponseStatus
    {
        /**
        * 2000：正常；
        * other：调用异常，具体异常内容见{@code msg}
        */
        [Description("Success")]
        SUCCESS = 2000,

        [Description("Failure")]
        FAILURE = 5000,

        [Description("Not Implemented")]
        NOT_IMPLEMENTED = 5001,

        [Description("Parameter incomplete")]
        PARAMETER_INCOMPLETE = 5002,

        [Description("Unsupported operation")]
        UNSUPPORTED = 5003, 

        [Description("AuthDefaultSource cannot be null")]
        NO_AUTH_SOURCE = 5004, 

        [Description("Unidentified platform")]
        UNIDENTIFIED_PLATFORM = 5005, 

        [Description("Illegal redirect uri")]
        ILLEGAL_REDIRECT_URI = 5006, 

        [Description("Illegal request")]
        ILLEGAL_REQUEST = 5007, 

        [Description("Illegal code")]
        ILLEGAL_CODE = 5008, 

        [Description("Illegal state")]
        ILLEGAL_STATUS = 5009, 

        [Description("The refresh token is required; it must not be null")]
        REQUIRED_REFRESH_TOKEN = 5010, 

        [Description("Invalid token")]
        ILLEGAL_TOKEN = 5011,

        [Description("Invalid key identifier(kid)")]
        ILLEGAL_KID = 5012,

        [Description("Invalid team id")]
        ILLEGAL_TEAM_ID = 5013,

        [Description("Invalid client id")]
        ILLEGAL_CLIENT_ID = 5014,

        [Description("Invalid client secret")]
        ILLEGAL_CLIENT_SECRET = 5015,

        [Description("Illegal wechat agent id")]
        ILLEGAL_WECHAT_AGENT_ID = 5016,
    }
}