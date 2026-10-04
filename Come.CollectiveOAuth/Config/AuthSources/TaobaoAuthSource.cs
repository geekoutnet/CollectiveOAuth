using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

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
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string revoke()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.TAOBAO.ToString();
        }
    }
}