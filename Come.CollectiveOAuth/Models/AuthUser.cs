using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Models
{
    public class AuthUser
    {
        /**
        * 用户第三方系统的唯一id。在调用方集成改组件时，可以用uuid + source唯一确定一个用户
        *
        * @since 1.3.3
        */
        public string uuid { get; set; }
        /**
         * 用户名
         */
        public string username { get; set; }
        /**
         * 用户昵称
         */
        public string nickname { get; set; }
        /**
         * 用户头像
         */
        public string avatar { get; set; }
        /**
         * 用户网址
         */
        public string blog { get; set; }
        /**
         * 所在公司
         */
        public string company { get; set; }
        /**
         * 位置
         */
        public string location { get; set; }
        /**
         * 用户邮箱
         */
        public string email { get; set; }
        /**
         * 用户备注（各平台中的用户个人介绍）
         */
        public string remark { get; set; }
        /**
         * 性别
         */
        public AuthUserGender gender { get; set; }
        /**
         * 用户来源
         */
        public string source { get; set; }
        /**
         * 用户授权的token信息
         */
        public AuthToken token { get; set; }

        /// <summary>
        /// 原有的用户信息(第三方返回的)
        /// </summary>
        public object originalUser { get; set; }

        public string originalUserStr { get; set; }
    }
}