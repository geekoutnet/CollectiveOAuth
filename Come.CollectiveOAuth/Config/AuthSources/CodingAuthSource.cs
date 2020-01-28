using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Coding扣钉
     */
    public class CodingAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://coding.net/oauth_authorize.html";
        }

        public string accessToken()
        {
            return "https://coding.net/api/oauth/access_token";
        }

        public string userInfo()
        {
            return "https://coding.net/api/account/current_user";
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
            return DefaultAuthSourceEnum.CODING.ToString();
        }
    }
}