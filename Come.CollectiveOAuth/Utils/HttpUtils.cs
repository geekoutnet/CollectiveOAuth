using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    public class HttpUtils
    {
        /// <summary>
        /// POST请求
        /// </summary>
        /// <param name="postUrl">The postUrl.</param>
        /// <param name="postData">The post data.</param>
        /// <returns>System.String.</returns>
        public static string RequestPost(string postUrl, string postData = null, string charset = null)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; //加上这一句
            if (string.IsNullOrWhiteSpace(charset))
            {
                charset = "UTF-8";
            }

            if (string.IsNullOrWhiteSpace(postData))
            {
                postData = "";
            }

            Stream outstream = null;
            Stream instream = null;
            StreamReader sr = null;
            HttpWebResponse response = null;
            HttpWebRequest request = null;
            Encoding encoding = System.Text.Encoding.GetEncoding(charset);
            
            byte[] data = encoding.GetBytes(postData);
            // 准备请求...
            try
            {
                // 设置参数
                request = WebRequest.Create(postUrl) as HttpWebRequest;
                CookieContainer cookieContainer = new CookieContainer();
                request.CookieContainer = cookieContainer;
                request.AllowAutoRedirect = true;
                request.Method = "POST";
                request.ContentType = "application/json;charset=" + charset.ToLower();
                request.ContentLength = data.Length;
                outstream = request.GetRequestStream();
                outstream.Write(data, 0, data.Length);
                outstream.Close();
                //发送请求并获取相应回应数据
                response = request.GetResponse() as HttpWebResponse;
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                instream = response.GetResponseStream();
                sr = new StreamReader(instream, encoding);
                //返回结果网页（html）代码
                string content = sr.ReadToEnd();
                string err = string.Empty;
                return content;
            }
            catch (Exception ex)
            {
                string err = ex.Message;
                return string.Empty;
            }
        }


        /// <summary>
        /// 发送Get请求
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="dic">请求参数定义</param>
        public static string RequestGet(string url, Dictionary<string, string> dicParams)
        {
            string result = "";
            StringBuilder builder = new StringBuilder();
            builder.Append(url);
            if (dicParams.Count > 0)
            {
                builder.Append("?");
                int i = 0;
                foreach (KeyValuePair<string, string> item in dicParams)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, item.Value);
                    i++;
                }
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(builder.ToString());
            //添加参数
            request.AllowAutoRedirect = true;
            request.Method = "GET";
            request.CookieContainer = new CookieContainer();
            request.ContentType = "application/json;charset=utf-8;";
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            Stream stream = resp.GetResponseStream();
            try
            {
                //获取内容
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                stream.Close();
            }
            return result;
        }


        /// <summary>
        /// github专用
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GithubRequestGet(string url)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(builder.ToString());
            request.UserAgent = "Foo";
            request.Accept = "application/json";
            return ComeRequestGet(request);
        }

        /// <summary>
        /// 发送Get请求
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="dic">请求参数定义</param>
        public static string RequestGet(string url)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(builder.ToString());
            request.ContentType = "application/json;charset=utf-8;";
            return ComeRequestGet(request);
        }

        /// <summary>
        /// 通用的get请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string ComeRequestGet(HttpWebRequest request)
        {
            string result = "";
            request.AllowAutoRedirect = true;
            request.Method = "GET";
            request.CookieContainer = new CookieContainer();
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            Stream stream = resp.GetResponseStream();
            try
            {
                //获取内容
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                stream.Close();
            }
            return result;
        }

    }
}