using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 今日头条
     */
    public class ToutiaoAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://open.snssdk.com/auth/authorize";
        }

        public string accessToken()
        {
            return "https://open.snssdk.com/auth/token";
        }

        public string userInfo()
        {
            return "https://open.snssdk.com/data/user_profile";
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
            return DefaultAuthSourceEnum.TOUTIAO.ToString();
        }
    }
}