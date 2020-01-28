using Come.CollectiveOAuth.Enums;

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
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            throw new System.NotImplementedException();
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.GITEE.ToString();
        }
    }
}