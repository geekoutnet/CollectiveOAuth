using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Gitee
     */
    public class GiteeAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://gitee.com/oauth/authorize";
        }

        public string accessToken()
        {
            return "https://gitee.com/oauth/token";
        }

        public string userInfo()
        {
            return "https://gitee.com/api/v5/user";
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
            return DefaultAuthSourceEnum.GITEE.ToString();
        }
    }
}