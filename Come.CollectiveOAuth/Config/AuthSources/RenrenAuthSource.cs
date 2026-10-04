using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 人人网
     */
    public class RenrenAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://graph.renren.com/oauth/authorize";
        }

        public string accessToken()
        {
            return "https://graph.renren.com/oauth/token";
        }

        public string userInfo()
        {
            return "https://api.renren.com/v2/user/get";
        }

        public string revoke()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            return "https://graph.renren.com/oauth/token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.RENREN.ToString();
        }
    }
}