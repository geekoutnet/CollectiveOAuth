using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 饿了么
     */
    public class ElemeAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://open-api.shop.ele.me/authorize";
        }

        public string accessToken()
        {
            return "https://open-api.shop.ele.me/token";
        }

        public string userInfo()
        {
            return "https://open-api.shop.ele.me/api/v1/";
        }

        public string revoke()
        {
            throw new System.NotImplementedException();
        }

        public string refresh()
        {
            return "https://open-api.shop.ele.me/token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.ELEME.ToString();
        }
    }
}