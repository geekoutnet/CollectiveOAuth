using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

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
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            return "https://oauth.kujiale.com/oauth2/auth/token/refresh";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.KUJIALE.ToString();
        }
    }
}