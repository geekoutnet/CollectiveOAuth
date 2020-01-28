using System;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Request
{
    public partial class DefaultAuthRequest : IAuthRequest
    {
        protected ClientConfig config;
        protected IAuthSource source;
        protected IAuthStateCache authStateCache { set; get; }

        public DefaultAuthRequest(ClientConfig config, IAuthSource source)
        {
            this.config = config;
            this.source = source;
            this.authStateCache = new DefaultAuthStateCache();
        }

        public DefaultAuthRequest(ClientConfig config, IAuthSource source, IAuthStateCache authStateCache)
        {
            this.config = config;
            this.source = source;
            this.authStateCache = authStateCache;
        }

        public virtual AuthResponse refresh(AuthToken authToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual AuthResponse revoke(AuthToken authToken)
        {
            throw new System.NotImplementedException();
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
        public virtual AuthResponse login(AuthCallback authCallback)
        {
            try
            {
                AuthChecker.checkCode(source, authCallback);
                AuthChecker.checkState(authCallback.state, source, authStateCache);

                AuthToken authToken = this.getAccessToken(authCallback);
                AuthUser user = this.getUserInfo(authToken);
                return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, user);
            }
            catch (Exception e)
            {
                return this.responseError(e);
            }
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
            if (string.IsNullOrWhiteSpace(state))
            {
                state = Guid.NewGuid().ToString();
            }
            // 缓存state
            authStateCache.cache(state, state);
            return state;
        }


        /**
         * 处理{@link AuthDefaultRequest#login(AuthCallback)} 发生异常的情况，统一响应参数
         *
         * @param e 具体的异常
         * @return AuthResponse
         */
        private AuthResponse responseError(Exception e)
        {
            int errorCode = Convert.ToInt32(AuthResponseStatus.FAILURE);
            string errorMsg = e.Message;
            return new AuthResponse(errorCode, errorMsg);
        }



        /**
        * 通用的 authorizationCode 协议
        *
        * @param code code码
        * @return HttpResponse
        */
        protected virtual string doPostAuthorizationCode(string code)
        {
            return HttpUtils.RequestPost(accessTokenUrl(code));
        }

        /**
         * 通用的 authorizationCode 协议
         *
         * @param code code码
         * @return HttpResponse
         */
        protected virtual string doGetAuthorizationCode(String code)
        {
            return HttpUtils.RequestGet(accessTokenUrl(code));
        }

        /**
         * 通用的 用户信息
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doPostUserInfo(AuthToken authToken)
        {
            return HttpUtils.RequestPost(userInfoUrl(authToken));
        }

        /**
         * 通用的 用户信息
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doGetUserInfo(AuthToken authToken)
        {
            return HttpUtils.RequestGet(userInfoUrl(authToken));
        }

        /**
         * 通用的post形式的取消授权方法
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doPostRevoke(AuthToken authToken)
        {
            return HttpUtils.RequestPost(revokeUrl(authToken));
        }

        /**
         * 通用的post形式的取消授权方法
         *
         * @param authToken token封装
         * @return HttpResponse
         */
        protected virtual string doGetRevoke(AuthToken authToken)
        {
            return HttpUtils.RequestGet(revokeUrl(authToken));
        }

    }
}