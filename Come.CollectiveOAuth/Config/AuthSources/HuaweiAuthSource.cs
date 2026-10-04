using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Config
{
    /**
     * 华为
     */
    public class HuaweiAuthSource : IAuthSource
    {
        public string authorize()
        {
            return "https://oauth-login.cloud.huawei.com/oauth2/v2/authorize";
        }

        public string accessToken()
        {
            return "https://oauth-login.cloud.huawei.com/oauth2/v2/token";
        }

        public string userInfo()
        {
            return "https://api.vmall.com/rest.php";
        }

        public string revoke()
        {
            throw new AuthException(AuthResponseStatus.UNSUPPORTED);
        }

        public string refresh()
        {
            return "https://oauth-login.cloud.huawei.com/oauth2/v2/token";
        }

        public string getName()
        {
            return DefaultAuthSourceEnum.HUAWEI.ToString();
        }
    }
}