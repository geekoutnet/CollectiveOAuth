using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 企业微信网页登录，对齐 JustAuth AuthWeChatEnterpriseWebRequest（JustAuth 平台名 WECHAT_ENTERPRISE_WEB）
     */
    public class WeChatEnterpriseAuthRequest : AbstractWeChatEnterpriseAuthRequest
    {
        public WeChatEnterpriseAuthRequest(ClientConfig config) : base(config, new WechatEnterpriseAuthSource())
        {
        }

        public WeChatEnterpriseAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new WechatEnterpriseAuthSource(), authStateCache)
        {
        }

        public override string authorize(string state)
        {
            // Java 先手动 urlEncode(redirectUri) 再交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            // Java 把 "#wechat_redirect" 拼进 state 参数值（state 为最后一个参数）；C# 拼在 build() 结果之后，避免被编码
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("appid", config.clientId)
                .queryParam("agentid", config.agentId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("response_type", "code")
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthWeChatEnterpriseWebScope.values())))
                .queryParam("state", getRealState(state))
                .build() + "#wechat_redirect";
        }
    }
}
