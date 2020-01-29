using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 淘宝
     */
    public class TaobaoAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://oauth.taobao.com/authorize";
        }

        public string accessToken()
        {
            return "https://oauth.taobao.com/token";
        }

        public string userInfo()
        {
            throw new System.NotImplementedException();
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
            return DefaultAuthSourceEnum.TAOBAO.ToString();
        }
    }
}