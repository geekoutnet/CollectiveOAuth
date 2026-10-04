using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 默认的request处理类，对齐 JustAuth AuthDefaultRequest
     */
    public partial class DefaultAuthRequest : IAuthRequest
    {
        protected ClientConfig config;
        protected IAuthSource source;
        protected IAuthStateCache authStateCache { set; get; }

        public DefaultAuthRequest(ClientConfig config, IAuthSource source) : this(config, source, new DefaultAuthStateCache())
        {
        }

        public DefaultAuthRequest(ClientConfig config, IAuthSource source, IAuthStateCache authStateCache)
        {
            this.config = config;
            this.source = source;
            this.authStateCache = authStateCache;
            if (!AuthChecker.isSupportedAuth(config, source))
            {
                throw new AuthException(AuthResponseStatus.PARAMETER_INCOMPLETE, source);
            }
            // 校验配置合法性
            this.checkConfig(config);
        }

        public virtual AuthResponse refresh(AuthToken authToken)
        {
            throw new AuthException(AuthResponseStatus.NOT_IMPLEMENTED);
        }

        public virtual AuthResponse revoke(AuthToken authToken)
        {
            throw new AuthException(AuthResponseStatus.NOT_IMPLEMENTED);
        }

        /**
         * 获取access token
         *
         * @param authCallback 授权成功后的回调参数
         * @return token
         * @see AuthDefaultRequest#authorize()
         * @see AuthDefaultRequest#authorize(String)
         */
        protected virtual AuthToken getAccessToken(AuthCallback authCallback)
        {
            throw new System.NotImplementedException();
        }

        /**
         * 使用token换取用户信息
         *
         * @param authToken token信息
         * @return 用户信息
         * @see AuthDefaultRequest#getAccessToken(AuthCallback)
         */
        protected virtual AuthUser getUserInfo(AuthToken authToken)
        {
            throw new System.NotImplementedException();
        }

        /**
         * 统一的登录入口。当通过{@link AuthDefaultRequest#authorize(String)}授权成功后，会跳转到调用方的相关回调方法中
         * 方法的入参可以使用{@code AuthCallback}，{@code AuthCallback}类中封装好了OAuth2授权回调所需要的参数
         *
         * @param authCallback 用于接收回调参数的实体
         * @return AuthResponse
         */
        public virtual AuthResponse login(AuthCallback authCallback)
        {
            try
            {
                checkCode(authCallback);
                if (!config.ignoreCheckState)
                {
                    AuthChecker.checkState(authCallback.state, source, authStateCache);
                    // 有意差异：state 一次性使用，校验通过立即删除，防止重放
                    if (!string.IsNullOrEmpty(authCallback.state))
                    {
                        authStateCache.remove(authCallback.state);
                    }
                }

                AuthToken authToken = this.getAccessToken(authCallback);
                AuthUser user = this.getUserInfo(authToken);
                return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, user);
            }
            catch (Exception e)
            {
                return this.responseError(e);
            }
        }

        protected virtual void checkCode(AuthCallback authCallback)
        {
            AuthChecker.checkCode(source, authCallback);
        }

        /**
         * 处理{@link AuthDefaultRequest#login(AuthCallback)} 发生异常的情况，统一响应参数
         *
         * @param e 具体的异常
         * @return AuthResponse
         */
        protected AuthResponse responseError(Exception e)
        {
            int errorCode = Convert.ToInt32(AuthResponseStatus.FAILURE);
            string errorMsg = e.Message;
            if (e is AuthException authException)
            {
                errorCode = authException.errorCode;
                if (!string.IsNullOrEmpty(authException.errorMsg))
                {
                    errorMsg = authException.errorMsg;
                }
            }
            return new AuthResponse(errorCode, errorMsg);
        }

        /**
         * 返回授权url，可自行跳转页面
         * <p>
         * 不建议使用该方式获取授权地址，不带{@code state}的授权地址，容易受到csrf攻击。
         * 建议使用{@link AuthDefaultRequest#authorize(String)}方法生成授权地址，在回调方法中对{@code state}进行校验
         *
         * @return 返回授权地址
         * @see AuthDefaultRequest#authorize(String)
         */
        public virtual string authorize()
        {
            return this.authorize(null);
        }

        /**
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         * @since 1.9.3
         */
        public virtual string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权码
         * @return 返回获取accessToken的url
         */
        protected virtual string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .queryParam("redirect_uri", config.redirectUri)
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param refreshToken refreshToken
         * @return 返回获取accessToken的url
         */
        protected virtual string refreshTokenUrl(string refreshToken)
        {
            return UrlBuilder.fromBaseUrl(source.refresh())
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("refresh_token", refreshToken)
                .queryParam("grant_type", "refresh_token")
                .queryParam("redirect_uri", config.redirectUri)
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken token
         * @return 返回获取userInfo的url
         */
        protected virtual string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo()).queryParam("access_token", authToken.accessToken).build();
        }

        /**
         * 返回获取revoke authorization的url
         *
         * @param authToken token
         * @return 返回获取revoke authorization的url
         */
        protected virtual string revokeUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.revoke()).queryParam("access_token", authToken.accessToken).build();
        }

        /**
         * 获取state，如果为空， 则默认取当前日期的时间戳
         *
         * @param state 原始的state
         * @return 返回不为null的state
         */
        protected virtual string getRealState(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                state = UuidUtils.getUUID();
            }
            // 缓存state
            authStateCache.cache(state, state);
            return state;
        }

        /**
         * 通用的 authorizationCode 协议
         *
         * @param code code码
         * @return HttpResponse
         */
        protected virtual string doPostAuthorizationCode(string code)
        {
            return HttpUtils.RequestPost(accessTokenUrl(code), httpConfig: config.httpConfig);
        }

        /**
         * 通用的 authorizationCode 协议
         *
         * @param code code码
         * @return HttpResponse
         */
        protected virtual string doGetAuthorizationCode(String code)
        {
            return HttpUtils.RequestGet(accessTokenUrl(code), httpConfig: config.httpConfig);
        }

        /**
         * 通用的 用户信息
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doPostUserInfo(AuthToken authToken)
        {
            return HttpUtils.RequestPost(userInfoUrl(authToken), httpConfig: config.httpConfig);
        }

        /**
         * 通用的 用户信息
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doGetUserInfo(AuthToken authToken)
        {
            return HttpUtils.RequestGet(userInfoUrl(authToken), httpConfig: config.httpConfig);
        }

        /**
         * 通用的post形式的取消授权方法
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doPostRevoke(AuthToken authToken)
        {
            return HttpUtils.RequestPost(revokeUrl(authToken), httpConfig: config.httpConfig);
        }

        /**
         * 通用的get形式的取消授权方法
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doGetRevoke(AuthToken authToken)
        {
            return HttpUtils.RequestGet(revokeUrl(authToken), httpConfig: config.httpConfig);
        }

        /**
         * 获取以 separator 分割过后的 scope 信息，与 JustAuth 一致；
         * 兼容旧配置：scopes 为空且配置了 scope 字符串时，直接使用该字符串
         *
         * @param separator     多个 scope 间的分隔符
         * @param encode        是否 encode 编码
         * @param defaultScopes 默认的 scope， 当客户端没有配置 scopes 时启用
         * @return String
         */
        protected string getScopes(string separator, bool encode, List<string> defaultScopes)
        {
            List<string> scopes = config.scopes;
            if (scopes == null || scopes.Count == 0)
            {
                if (!string.IsNullOrEmpty(config.scope))
                {
                    return encode ? GlobalAuthUtil.urlEncode(config.scope) : config.scope;
                }
                if (defaultScopes == null || defaultScopes.Count == 0)
                {
                    return "";
                }
                scopes = defaultScopes;
            }
            if (separator == null)
            {
                // 默认为空格
                separator = " ";
            }
            string scopeStr = string.Join(separator, scopes);
            return encode ? GlobalAuthUtil.urlEncode(scopeStr) : scopeStr;
        }

        /**
         * 校验配置合法性
         *
         * @param config 配置
         */
        protected virtual void checkConfig(ClientConfig config)
        {
            AuthChecker.checkConfig(config, source);
        }
    }
}
