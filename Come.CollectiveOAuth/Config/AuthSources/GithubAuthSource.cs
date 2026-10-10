using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Github
     */
    public class GithubAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://github.com/login/oauth/authorize";
        }

        public string accessToken()
        {
            return "https://github.com/login/oauth/access_token";
        }

        public string userInfo()
        {
            return "https://api.github.com/user";
        }

        public string revoke()
        {
            throw new Come.CollectiveOAuth.Exceptions.AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            throw new Come.CollectiveOAuth.Exceptions.AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.GITHUB.ToString();
        }
    }
}