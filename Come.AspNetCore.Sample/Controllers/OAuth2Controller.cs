using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Come.CollectiveOAuth.Utils;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Enums;
using Newtonsoft.Json;

namespace Come.AspNetCore.Sample.Controllers
{
    public class OAuth2Controller : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAuthStateCache _authStateCache;

        public OAuth2Controller(ILogger<HomeController> logger, IAuthStateCache authStateCache)
        {
            _logger = logger;
            _authStateCache = authStateCache;
        }

        private AuthRequestFactory CreateFactory()
        {
            return new AuthRequestFactory(_authStateCache);
        }

        /// <summary>
        /// 授权平台不存在或暂不支持时，返回统一格式的错误信息
        /// </summary>
        private IActionResult UnsupportedResult(Exception e)
        {
            var authResponse = new AuthResponse(Convert.ToInt32(AuthResponseStatus.UNSUPPORTED), e.Message);
            return Content(JsonConvert.SerializeObject(authResponse));
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 构建授权Url方法
        /// </summary>
        /// <param name="authSource"></param>
        /// <returns>RedirectUrl</returns>
        public IActionResult Authorization(string authSource)
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
        public IActionResult Callback(string authSource, AuthCallback authCallback)
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
        public IActionResult DingTalkCallback(AuthCallback authCallback)
        {
            var request = CreateFactory().getRequest("DINGTALK_SCAN");
            var authResponse = request.login(authCallback);
            return Content(JsonConvert.SerializeObject(authResponse));
        }
    }
}
