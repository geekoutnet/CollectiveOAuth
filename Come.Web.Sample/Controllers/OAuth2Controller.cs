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

        /// <summary>
        /// 构建授权Url方法
        /// </summary>
        /// <param name="authSource"></param>
        /// <returns>RedirectUrl</returns>
        public ActionResult Authorization(string authSource)
        {
            AuthRequestFactory authRequest = new AuthRequestFactory();
            var request = authRequest.getRequest(authSource);
            var authorize = request.authorize(AuthStateUtils.createState());
            Console.WriteLine(authorize);
            return Redirect(authorize);
        }

        /// <summary>
        /// 授权回调方法
        /// </summary>
        /// <param name="authSource"></param>
        /// <param name="authCallback"></param>
        /// <returns></returns>
        public ActionResult Callback(string authSource, AuthCallback authCallback)
        {
            AuthRequestFactory authRequest = new AuthRequestFactory();
            var request = authRequest.getRequest(authSource);
            var authResponse = request.login(authCallback);
            return Content(JsonConvert.SerializeObject(authResponse));
        }

        /// <summary>
        /// 钉钉callback
        /// </summary>
        /// <param name="authSource"></param>
        /// <param name="authCallback"></param>
        /// <returns></returns>
        public ActionResult DingTalkCallback(AuthCallback authCallback)
        {
            AuthRequestFactory authRequest = new AuthRequestFactory();
            var request = authRequest.getRequest("DINGTALK_SCAN");
            var authResponse = request.login(authCallback);
            return Content(JsonConvert.SerializeObject(authResponse));
        }
    }
}