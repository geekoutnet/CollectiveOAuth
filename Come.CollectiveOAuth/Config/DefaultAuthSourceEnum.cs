using System.ComponentModel;

namespace Come.CollectiveOAuth.Enums
{
    public enum DefaultAuthSourceEnum
    {
        [Description("微信公众平台")]
        WECHAT_MP,

        [Description("微信开放平台")]
        WECHAT_OPEN,

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

        [Description("微博")]
        WEIBO,

        [Description("腾讯QQ")]
        QQ,

        [Description("抖音")]
        DOUYIN,

        [Description("Google(谷歌)")]
        GOOGLE,

        [Description("Facebook")]
        FACEBOOK,

        [Description("微软")]
        MICROSOFT,

        [Description("今日头条")]
        TOUTIAO,

        [Description("Teambition")]
        TEAMBITION,

        [Description("人人网")]
        RENREN,

        [Description("Pinterest")]
        PINTEREST,

        [Description("Stack Overflow")]
        STACK_OVERFLOW,

        [Description("华为")]
        HUAWEI,

        [Description("酷家乐")]
        KUJIALE,

        [Description("Gitlab")]
        GITLAB,

        [Description("美团")]
        MEITUAN,

        [Description("饿了么")]
        ELEME,

        [Description("Twitter")]
        TWITTER,

        [Description("淘宝")]
        TAOBAO,

    }
}