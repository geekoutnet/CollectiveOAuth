using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 美团
     */
    public class MeituanAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://openapi.waimai.meituan.com/oauth/authorize";
        }

        public string accessToken()
        {
            return "https://openapi.waimai.meituan.com/oauth/access_token";
        }

        public string userInfo()
        {
            return "https://openapi.waimai.meituan.com/oauth/userinfo";
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            return "https://openapi.waimai.meituan.com/oauth/refresh_token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.MEITUAN.ToString();
        }
    }
}