using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 酷家乐
     */
    public class KujialeAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://oauth.kujiale.com/oauth2/show";
        }

        public string accessToken()
        {
            return "https://oauth.kujiale.com/oauth2/auth/token";
        }

        public string userInfo()
        {
            return "https://oauth.kujiale.com/oauth2/openapi/user";
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            return "https://oauth-login.cloud.huawei.com/oauth2/v2/token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.KUJIALE.ToString();
        }
    }
}