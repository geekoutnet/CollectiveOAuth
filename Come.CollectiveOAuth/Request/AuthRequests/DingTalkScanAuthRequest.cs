using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 钉钉二维码登录，对齐 JustAuth AuthDingTalkRequest
     */
    public class DingTalkScanAuthRequest : AbstractDingTalkAuthRequest
    {
        public DingTalkScanAuthRequest(ClientConfig config) : base(config, new DingTalkScanAuthSource())
        {
        }

        public DingTalkScanAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new DingTalkScanAuthSource(), authStateCache)
        {
        }
    }
}
