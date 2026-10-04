using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

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
            return "https://api.twitter.com/1.1/account/verify_credentials.json";
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
            return DefaultAuthSourceEnum.TWITTER.ToString();
        }
    }
}