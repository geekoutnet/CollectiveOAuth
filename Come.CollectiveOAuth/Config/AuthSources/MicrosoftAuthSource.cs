using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 微软
     */
    public class MicrosoftAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://login.microsoftonline.com/%s/oauth2/v2.0/authorize";
        }

        public string accessToken()
        {
            return "https://login.microsoftonline.com/%s/oauth2/v2.0/token";
        }

        public string userInfo()
        {
            return "https://graph.microsoft.com/v1.0/me";
        }

        public string revoke()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            return "https://login.microsoftonline.com/%s/oauth2/v2.0/token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.MICROSOFT.ToString();
        }
    }
}