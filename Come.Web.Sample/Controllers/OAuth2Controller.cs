using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Come.Web.Sample.Controllers
{
    public class OAuth2Controller : Controller
    {
        private AuthRequestFactory CreateFactory()
        {
            return new AuthRequestFactory();
        }

        /// <summary>
        /// 授权平台不存在或暂不支持时，返回统一格式的错误信息
        /// </summary>
        private ActionResult UnsupportedResult(Exception e)
        {
            // 库内抛出的 AuthException 保留其错误码（如配置不全 5002），其余按不支持的平台处理
            var code = e is AuthException authException ? authException.errorCode : Convert.ToInt32(AuthResponseStatus.UNSUPPORTED);
            var authResponse = new AuthResponse(code, e.Message);
            return Content(JsonConvert.SerializeObject(authResponse));
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
                var authorize = request.authorize(AuthStateUtils.createState());
                Console.WriteLine(authorize);
                return Redirect(authorize);
            }
            catch (Exception e)
            {
                return UnsupportedResult(e);
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
            try
            {
                var request = CreateFactory().getRequest(authSource);
                var authResponse = request.login(authCallback);
                return Content(JsonConvert.SerializeObject(authResponse));
            }
            catch (Exception e)
            {
                return UnsupportedResult(e);
            }
        }

        /// <summary>
        /// 钉钉callback
        /// </summary>
        /// <param name="authSource"></param>
        /// <param name="authCallback"></param>
        /// <returns></returns>
        public ActionResult DingTalkCallback(AuthCallback authCallback)
        {
            var request = CreateFactory().getRequest("DINGTALK_SCAN");
            var authResponse = request.login(authCallback);
            return Content(JsonConvert.SerializeObject(authResponse));
        }


        /// Eleme验证授权
        /// </summary>
        /// <param name="authSource"></param>
        /// <param name="authCallback"></param>
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