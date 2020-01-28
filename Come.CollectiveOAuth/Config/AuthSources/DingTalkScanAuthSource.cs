using Come.CollectiveOAuth.Enums;

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
            throw new System.NotImplementedException(AuthResponseStatus.UNSUPPORTED.GetDesc());
        }

        public string userInfo()
        {
            return "https://oapi.dingtalk.com/sns/getuserinfo_bycode"; ;
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            throw new System.NotImplementedException();
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.DINGTALK_SCAN.ToString();
        }
    }
}