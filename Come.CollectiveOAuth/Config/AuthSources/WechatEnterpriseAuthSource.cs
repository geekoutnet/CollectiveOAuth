using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 企业微信
     */
    public class WechatEnterpriseAuthSource : IAuthSource
    {
        
        public string accessToken()
        {
            return "https://qyapi.weixin.qq.com/cgi-bin/gettoken";
        }

        public string authorize()
        {
            //return "https://open.work.weixin.qq.com/wwopen/sso/qrConnect";
            return "https://open.weixin.qq.com/connect/oauth2/authorize";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.WECHAT_ENTERPRISE.ToString();
        }

        public string refresh()
        {
            throw new System.NotImplementedException();
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string userInfo()
        {
            return "https://qyapi.weixin.qq.com/cgi-bin/user/getuserinfo";
        }
    }
}