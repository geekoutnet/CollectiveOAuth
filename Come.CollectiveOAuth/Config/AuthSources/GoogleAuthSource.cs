using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Google(谷歌)
     */
    public class GoogleAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://accounts.google.com/o/oauth2/v2/auth";
        }

        public string accessToken()
        {
            return "https://www.googleapis.com/oauth2/v4/token";
        }

        public string userInfo()
        {
            return "https://www.googleapis.com/oauth2/v3/userinfo";
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
            return DefaultAuthSourceEnum.GOOGLE.ToString();
        }
    }
}