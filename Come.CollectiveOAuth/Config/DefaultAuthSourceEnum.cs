using System.ComponentModel;

namespace Come.CollectiveOAuth.Enums
{
    public enum DefaultAuthSourceEnum
    {
        [Description("微信公众平台")]
        WECHAT_MP,

        [Description("企业微信自动授权")]
        WECHAT_ENTERPRISE,

        [Description("企业微信扫码")]
        WECHAT_ENTERPRISE_SCAN,

        [Description("支付宝服务窗")]
        ALIPAY_MP,

        [Description("码云授权")]
        GITEE,

        [Description("Github授权")]
        GITHUB,

        [Description("百度开放平台")]
        BAIDU,

        [Description("小米开放平台")]
        XIAOMI,

        [Description("钉钉扫码")]
        DINGTALK_SCAN,

        [Description("OSChina开源中国")]
        OSCHINA,

        [Description("Coding扣钉")]
        CODING,

        [Description("LinkedIn领英")]
        LINKEDIN,

    }
}