using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Teambition
     */
    public class TeambitionAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://account.teambition.com/oauth2/authorize";
        }

        public string accessToken()
        {
            return "https://account.teambition.com/oauth2/access_token";
        }

        public string userInfo()
        {
            return "https://api.teambition.com/users/me";
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            return "https://account.teambition.com/oauth2/refresh_token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.TEAMBITION.ToString();
        }
    }
}