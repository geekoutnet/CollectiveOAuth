using System;

namespace Come.CollectiveOAuth.Utils
{
    public class AuthStateUtils
    {
        /**
         * 生成随机state，采用https://github.com/lets-mica/mica的UUID工具
         *
         * @return 随机的state字符串
         */
        public static string createState()
        {
            return Guid.NewGuid().ToString();
        }
    }
}