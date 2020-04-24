using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 微博
     */
    public class WeiboAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://api.weibo.com/oauth2/authorize";
        }

        public string accessToken()
        {
            return "https://api.weibo.com/oauth2/access_token";
        }

        public string userInfo()
        {
            return "https://api.weibo.com/2/users/show.json";
        }

        public string revoke()
        {
            return "https://api.weibo.com/oauth2/revokeoauth2";
        }

        public string refresh()
        {
            throw new System.NotImplementedException();
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.WEIBO.ToString();
        }
    }
}