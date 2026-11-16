using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Come.Web.Sample.Controllers
{
    public class OAuth2Controller : Controller
    {
        /// <summary>
        /// 保存授权 state 的 cookie 名，用于回调时防御登录 CSRF
        /// </summary>
        private const string StateCookieName = "oauth_state";

        private AuthRequestFactory CreateFactory()
        {
            return new AuthRequestFactory();
        }

        /// <summary>
        /// 统一的错误返回：只有业务异常（不支持的平台、库抛出的 AuthException）回显错误信息，其余异常只记录日志并返回通用提示
        /// </summary>
        private ActionResult ErrorResult(Exception e)
        {
            AuthResponse authResponse;
            if (e is AuthException authException)
            {
                // 库内抛出的 AuthException 保留其错误码（如配置不全 5002）
                authResponse = new AuthResponse(authException.errorCode, e.Message);
            }
            else if (e is NotSupportedException)
            {
                authResponse = new AuthResponse(Convert.ToInt32(AuthResponseStatus.UNSUPPORTED), e.Message);
            }
            else
            {
                Trace.TraceError("OAuth 处理失败: {0}", e);
                authResponse = new AuthResponse(Convert.ToInt32(AuthResponseStatus.FAILURE), "服务器内部错误，请稍后重试");
            }
            return Content(JsonConvert.SerializeObject(authResponse));
        }

        /// <summary>
        /// 只返回非敏感字段。
        /// 注意：生产环境中 access_token / refresh_token 等令牌应只保存在服务端（会话或数据库），不要返回给浏览器。
        /// </summary>
        private static object ToSafeResponse(AuthResponse authResponse)
        {
            var user = authResponse.data as AuthUser;
            return new
            {
                authResponse.code,
                authResponse.msg,
                data = user == null ? null : new { user.uuid, user.username, user.nickname, user.avatar, user.source }
            };
        }

        /// <summary>
        /// 写入或删除 state cookie。
        /// .NET Framework 4.6.2 的 HttpCookie 不支持 SameSite 属性（4.7.2 起才有），因此直接写 Set-Cookie 头。
        /// </summary>
        private void SetStateCookie(string value, bool delete = false)
        {
            var cookie = StateCookieName + "=" + (delete ? "" : value) + "; Path=/; HttpOnly; SameSite=Lax"
                + (delete ? "; Expires=Thu, 01 Jan 1970 00:00:00 GMT" : "; Max-Age=600")
                + (Request.IsSecureConnection ? "; Secure" : "");
            Response.AppendHeader("Set-Cookie", cookie);
        }

        /// <summary>
        /// 校验回调 state 与授权时写入 cookie 的 state 一致后再登录
        /// </summary>
        private ActionResult LoginWithStateCheck(string authSource, AuthCallback authCallback)
        {
            try
            {
                var expectedState = Request.Cookies[StateCookieName]?.Value;
                if (string.IsNullOrEmpty(expectedState) || authCallback == null
                    || !string.Equals(expectedState, authCallback.state, StringComparison.Ordinal))
                {
                    var illegal = new AuthResponse(Convert.ToInt32(AuthResponseStatus.ILLEGAL_STATUS), "state 校验失败，请重新发起登录");
                    return Content(JsonConvert.SerializeObject(illegal));
                }
                SetStateCookie(null, delete: true);

                var request = CreateFactory().getRequest(authSource);
                var authResponse = request.login(authCallback);
                return Content(JsonConvert.SerializeObject(ToSafeResponse(authResponse)));
            }
            catch (Exception e)
            {
                return ErrorResult(e);
            }
        }

        /// <summary>
        /// 构建授权Url方法
        /// </summary>
        /// <param name="authSource"></param>
        /// <returns>RedirectUrl</returns>
        public ActionResult Authorization(string authSource)
        {
            try
            {
                var request = CreateFactory().getRequest(authSource);
                var state = AuthStateUtils.createState();
                var authorize = request.authorize(state);
                // state 写入 HttpOnly + SameSite=Lax cookie，回调时比对，防御登录 CSRF
                SetStateCookie(state);
                Trace.WriteLine("授权地址: " + authorize);
                return Redirect(authorize);
            }
            catch (Exception e)
            {
                return ErrorResult(e);
            }
        }

        /// <summary>
        /// 授权回调方法
        /// </summary>
        /// <param name="authSource"></param>
        /// <param name="authCallback"></param>
        /// <returns></returns>
        public ActionResult Callback(string authSource, AuthCallback authCallback)
        {
            return LoginWithStateCheck(authSource, authCallback);
        }

        /// <summary>
        /// 钉钉callback
        /// </summary>
        /// <param name="authCallback"></param>
        /// <returns></returns>
        public ActionResult DingTalkCallback(AuthCallback authCallback)
        {
            return LoginWithStateCheck("DINGTALK_SCAN", authCallback);
        }


        /// <summary>
        /// Eleme验证授权
        /// </summary>
        /// <returns></returns>
        public ActionResult ElemeVerification()
        {
            var retMsg = new
            {
                message = "ok"
            };
            return Json(retMsg, JsonRequestBehavior.AllowGet);
        }
    }
}
