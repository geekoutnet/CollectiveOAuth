using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Pinterest
     */
    public class PinterestAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://api.pinterest.com/oauth";
        }

        public string accessToken()
        {
            return "https://api.pinterest.com/v1/oauth/token";
        }

        public string userInfo()
        {
            return "https://api.pinterest.com/v1/me";
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
            return DefaultAuthSourceEnum.PINTEREST.ToString();
        }
    }
}