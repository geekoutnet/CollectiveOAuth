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

        public ActionResult Authorization(string authSource)
        {
            if (authSource.IsNullOrEmpty())
            {
                authSource = "WECHAT_MP";
            }

            AuthRequestFactory authRequest = new AuthRequestFactory();
            var request = authRequest.getRequest(authSource);
            var authorize = request.authorize(AuthStateUtils.createState());
            Console.WriteLine(authorize);
            return Redirect(authorize);
        }

        /// <summary>
        /// 通用的callback
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