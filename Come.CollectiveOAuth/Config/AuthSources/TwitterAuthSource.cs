using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Twitter
     */
    public class TwitterAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://api.twitter.com/oauth/authenticate";
        }

        public string accessToken()
        {
            return "https://api.twitter.com/oauth/access_token";
        }

        public string userInfo()
        {
            return "https://api.twitter.com/1.1/users/show.json";
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
            return DefaultAuthSourceEnum.TWITTER.ToString();
        }
    }
}