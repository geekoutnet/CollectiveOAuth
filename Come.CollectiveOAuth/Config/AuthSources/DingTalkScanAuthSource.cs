using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 钉钉扫码
     */
    public class DingTalkScanAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://oapi.dingtalk.com/connect/qrconnect";
        }

        public string accessToken()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string userInfo()
        {
            return "https://oapi.dingtalk.com/sns/getuserinfo_bycode";
        }

        public string revoke()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.DINGTALK_SCAN.ToString();
        }
    }
}