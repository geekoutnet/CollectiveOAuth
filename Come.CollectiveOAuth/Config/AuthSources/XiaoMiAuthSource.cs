using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 小米开放平台
     */
    public class XiaoMiAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://account.xiaomi.com/oauth2/authorize";
        }

        public string accessToken()
        {
            return "https://account.xiaomi.com/oauth2/token";
        }

        public string userInfo()
        {
            return "https://open.account.xiaomi.com/user/profile";
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            return "https://account.xiaomi.com/oauth2/token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.XIAOMI.ToString();
        }
    }
}