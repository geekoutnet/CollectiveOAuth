using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 微软登录，对齐 JustAuth AuthMicrosoftRequest
     */
    public class MicrosoftAuthRequest : AbstractMicrosoftAuthRequest
    {
        public MicrosoftAuthRequest(ClientConfig config) : base(config, new MicrosoftAuthSource())
        {
        }

        public MicrosoftAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new MicrosoftAuthSource(), authStateCache)
        {
        }

        protected override void checkConfig(ClientConfig config)
        {
            base.checkConfig(config);
            // 微软的回调地址必须为https的链接或者localhost,不允许使用http
            if (!GlobalAuthUtil.isHttpsProtocolOrLocalHost(config.redirectUri))
            {
                // Microsoft's redirect uri must use the HTTPS or localhost
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
        }
    }
}
