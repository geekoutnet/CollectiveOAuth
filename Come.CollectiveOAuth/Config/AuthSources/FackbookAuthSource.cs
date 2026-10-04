using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Facebook
     */
    public class FackbookAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://www.facebook.com/v18.0/dialog/oauth";
        }

        public string accessToken()
        {
            return "https://graph.facebook.com/v18.0/oauth/access_token";
        }

        public string userInfo()
        {
            return "https://graph.facebook.com/v18.0/me";
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
            return DefaultAuthSourceEnum.FACEBOOK.ToString();
        }
    }
}