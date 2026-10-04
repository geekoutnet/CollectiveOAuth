using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Linkin领英
     */
    public class LinkedInAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://www.linkedin.com/oauth/v2/authorization";
        }

        public string accessToken()
        {
            return "https://www.linkedin.com/oauth/v2/accessToken";
        }

        public string userInfo()
        {
            return "https://api.linkedin.com/v2/me";
        }

        public string revoke()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            return "https://www.linkedin.com/oauth/v2/accessToken";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.LINKEDIN.ToString();
        }
    }
}