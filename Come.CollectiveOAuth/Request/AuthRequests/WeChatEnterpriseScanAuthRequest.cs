using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 企业微信二维码登录，对齐 JustAuth AuthWeChatEnterpriseQrcodeRequest（JustAuth 平台名 WECHAT_ENTERPRISE）
     */
    public class WeChatEnterpriseScanAuthRequest : AbstractWeChatEnterpriseAuthRequest
    {
        public WeChatEnterpriseScanAuthRequest(ClientConfig config) : base(config, new WechatEnterpriseScanAuthSource())
        {
        }

        public WeChatEnterpriseScanAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new WechatEnterpriseScanAuthSource(), authStateCache)
        {
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("appid", config.clientId)
                .queryParam("agentid", config.agentId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .queryParam("lang", config.lang)
                .build();
        }
    }
}
