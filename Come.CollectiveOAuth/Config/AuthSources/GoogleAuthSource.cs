using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Google(谷歌)
     */
    public class GoogleAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://accounts.google.com/o/oauth2/v2/auth";
        }

        public string accessToken()
        {
            return "https://oauth2.googleapis.com/token";
        }

        public string userInfo()
        {
            return "https://openidconnect.googleapis.com/v1/userinfo";
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
            return DefaultAuthSourceEnum.GOOGLE.ToString();
        }
    }
}