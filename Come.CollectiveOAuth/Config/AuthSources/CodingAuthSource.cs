using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * Coding扣钉，地址与 JustAuth 一致，%s 为 domainPrefix（团队域名前缀）
     */
    public class CodingAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://%s.coding.net/oauth_authorize.html";
        }

        public string accessToken()
        {
            return "https://%s.coding.net/api/oauth/access_token";
        }

        public string userInfo()
        {
            return "https://%s.coding.net/api/account/current_user";
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
            return DefaultAuthSourceEnum.CODING.ToString();
        }
    }
}
