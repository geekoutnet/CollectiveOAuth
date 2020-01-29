using System.ComponentModel;

namespace Come.CollectiveOAuth.Enums
{
    /**
     * 今日头条授权登录时的异常状态码
     *
     * @author wei.fu (wei.fu@rthinkingsoft.cn)
     * @since 1.8
     */
    public enum AuthToutiaoErrorCode
    {
        /**
         * 0：正常；
         * other：调用异常，具体异常内容见{@code desc}
         */
        [Description("接口调用成功")]
        EC0 = 0, 

        [Description("API配置错误，未传入Client Key")]
        EC1 = 1, 

        [Description("API配置错误，Client Key错误，请检查是否和开放平台的ClientKey一致")]
        EC2 = 2, 

        [Description("没有授权信息")]
        EC3 = 3, 

        [Description("响应类型错误")]
        EC4 = 4, 

        [Description("授权类型错误")]
        EC5 = 5, 

        [Description("client_secret错误")]
        EC6 = 6, 

        [Description("authorize_code过期")]
        EC7 = 7, 

        [Description("指定url的scheme不是https")]
        EC8 = 8, 

        [Description("接口内部错误，请联系头条技术")]
        EC9 = 9, 

        [Description("access_token过期")]
        EC10 = 10, 

        [Description("缺少access_token")]
        EC11 = 11, 

        [Description("参数缺失")]
        EC12 = 12, 

        [Description("url错误")]
        EC13 = 13, 

        [Description("域名与登记域名不匹配")]
        EC21 = 21, 

        [Description("未知错误，请联系头条技术")]
        EC999 = 999, 
    }
}