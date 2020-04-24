using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Gitlab
     */
    public class GitlabAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://gitlab.com/oauth/authorize";
        }

        public string accessToken()
        {
            return "https://gitlab.com/oauth/token";
        }

        public string userInfo()
        {
            return "https://gitlab.com/api/v4/user";
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
            return DefaultAuthSourceEnum.GITLAB.ToString();
        }
    }
}