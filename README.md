<p align="center">
  <img src="docs/images/logo.png" width="360" alt="CollectiveOAuth">
</p>

<p align="center">
  <strong>.NET 平台的第三方 OAuth 登录聚合库</strong><br>
  一套接口接入微信、支付宝、钉钉、QQ、GitHub、Google 等 31 个平台
</p>

<p align="center">
  <a href="https://geekoutnet.github.io/CollectiveOAuth/"><img src="https://img.shields.io/badge/文档-在线阅读-4b32c3" alt="文档"></a>
  <img src="https://img.shields.io/badge/.NET%20Framework-4.6.2+-512bd4" alt=".NET Framework 4.6.2+">
  <img src="https://img.shields.io/badge/.NET%20Standard-2.0-512bd4" alt=".NET Standard 2.0">
  <img src="https://img.shields.io/badge/.NET-8.0-512bd4" alt=".NET 8">
  <img src="https://img.shields.io/badge/对齐-JustAuth%201.16.7-orange" alt="JustAuth 1.16.7">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-AGPL--3.0-blue" alt="AGPL-3.0"></a>
</p>

<p align="center">
  <a href="https://geekoutnet.github.io/CollectiveOAuth/">使用文档</a> ·
  <a href="https://github.com/geekoutnet/CollectiveOAuth">GitHub</a> ·
  <a href="https://gitee.com/rthinking/CollectiveOAuth">Gitee</a> ·
  <a href="README.en.md">English</a>
</p>

---

## 简介

CollectiveOAuth 把各家平台五花八门的 OAuth 协议细节封装起来。无论接入哪个平台，都只需要两个调用：

```text
你的应用 ──authorize()──▶ 第三方授权页 ──回调──▶ login() ──▶ AuthUser（统一的用户信息）
```

项目以 Java 社区成熟的 [JustAuth](https://github.com/justauth/JustAuth) 1.16.7 为基准实现。每个平台的授权地址、发出的请求和解析出的用户信息，都与 JustAuth 做过逐项的自动化比对测试。

## 特性

- **覆盖面广**：31 个国内外平台，国内包括微信、企业微信、支付宝、钉钉、QQ、微博、抖音、华为、淘宝等，海外包括 GitHub、Google、Microsoft、Facebook、Twitter、LinkedIn 等。
- **API 统一**：所有平台共用 `ClientConfig`、`AuthCallback`、`AuthResponse`、`AuthUser`，切换平台只需更换请求类。
- **安全默认值**：state 一次性使用，防止 CSRF 和回调重放；授权地址参数自动 URL 编码。
- **支持多实例部署**：提供基于 `IDistributedCache` 的 state 缓存，可接入 Redis。
- **可控的网络行为**：支持按平台设置超时和代理；第三方返回错误时给出状态码和响应内容，便于排查。
- **跨框架**：同时支持 .NET Framework 4.6.2+、.NET Standard 2.0 和 .NET 8。
- **测试保障**：236 个单元测试与差分测试，覆盖全部 31 个平台。

## 支持的平台

| | 平台 | 平台名（authSource） | 请求类 | 官方文档 |
|:-:|---|---|---|:-:|
| <img src="docs/images/platforms/wechat.png" width="18"> | 微信公众号 | `WECHAT_MP` | [WeChatMpAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/WeChatMpAuthRequest.cs) | [文档](https://developers.weixin.qq.com/doc/offiaccount/OA_Web_Apps/Wechat_webpage_authorization.html) |
| <img src="docs/images/platforms/wechat.png" width="18"> | 微信开放平台 | `WECHAT_OPEN` | [WeChatOpenAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/WeChatOpenAuthRequest.cs) | [文档](https://developers.weixin.qq.com/doc/oplatform/Website_App/WeChat_Login/Wechat_Login.html) |
| <img src="docs/images/platforms/wechat.png" width="18"> | 企业微信网页授权 | `WECHAT_ENTERPRISE` | [WeChatEnterpriseAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/WeChatEnterpriseAuthRequest.cs) | [文档](https://developer.work.weixin.qq.com/document/path/91022) |
| <img src="docs/images/platforms/wechat.png" width="18"> | 企业微信扫码 | `WECHAT_ENTERPRISE_SCAN` | [WeChatEnterpriseScanAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/WeChatEnterpriseScanAuthRequest.cs) | [文档](https://developer.work.weixin.qq.com/document/path/91019) |
| <img src="docs/images/platforms/alipay.png" width="18"> | 支付宝 | `ALIPAY_MP` | [AlipayMpAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/AlipayMpAuthRequest.cs) | [文档](https://opendocs.alipay.com/open/284/web) |
| <img src="docs/images/platforms/dingtalk.png" width="18"> | 钉钉扫码 | `DINGTALK_SCAN` | [DingTalkScanAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/DingTalkScanAuthRequest.cs) | [文档](https://open.dingtalk.com/document/orgapp/tutorial-obtaining-user-personal-information) |
| <img src="docs/images/platforms/qq.png" width="18"> | QQ | `QQ` | [QQAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/QQAuthRequest.cs) | [文档](https://wiki.connect.qq.com/%E4%BD%BF%E7%94%A8authorization_code%E8%8E%B7%E5%8F%96access_token) |
| <img src="docs/images/platforms/weibo.png" width="18"> | 微博 | `WEIBO` | [WeiboAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/WeiboAuthRequest.cs) | [文档](https://open.weibo.com/wiki/%E6%8E%88%E6%9D%83%E6%9C%BA%E5%88%B6%E8%AF%B4%E6%98%8E) |
| <img src="docs/images/platforms/baidu.png" width="18"> | 百度 | `BAIDU` | [BaiduAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/BaiduAuthRequest.cs) | [文档](http://developer.baidu.com/wiki/index.php?title=docs/oauth) |
| <img src="docs/images/platforms/gitee.png" width="18"> | 码云 Gitee | `GITEE` | [GiteeAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/GiteeAuthRequest.cs) | [文档](https://gitee.com/api/v5/oauth_doc) |
| <img src="docs/images/platforms/oschina.png" width="18"> | 开源中国 | `OSCHINA` | [OschinaAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/OschinaAuthRequest.cs) | [文档](https://www.oschina.net/openapi/docs/oauth2_authorize) |
| <img src="docs/images/platforms/coding.png" width="18"> | Coding | `CODING` | [CodingAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/CodingAuthRequest.cs) | [文档](https://coding.net/help/openapi) |
| <img src="docs/images/platforms/mi.png" width="18"> | 小米 | `XIAOMI` | [XiaoMiAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/XiaoMiAuthRequest.cs) | [文档](https://dev.mi.com/console/doc/detail?pId=711) |
| <img src="docs/images/platforms/huawei.png" width="18"> | 华为 | `HUAWEI` | [HuaweiAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/HuaweiAuthRequest.cs) | [文档](https://developer.huawei.com/consumer/cn/doc/development/HMSCore-Guides/open-platform-oauth-0000001050123437) |
| <img src="docs/images/platforms/douyin.png" width="18"> | 抖音 | `DOUYIN` | [DouyinAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/DouYinAuthRequest.cs) | [文档](https://developer.open-douyin.com/docs/resource/zh-CN/dop/develop/openapi/account-permission/get-permission-code) |
| <img src="docs/images/platforms/toutiao.png" width="18"> | 今日头条 | `TOUTIAO` | [ToutiaoAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/ToutiaoAuthRequest.cs) | [文档](https://open.mp.toutiao.com/) |
| <img src="docs/images/platforms/meituan.png" width="18"> | 美团 | `MEITUAN` | [MeituanAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/MeituanAuthRequest.cs) | [文档](http://open.waimai.meituan.com/openapi_docs/oauth/) |
| <img src="docs/images/platforms/eleme.png" width="18"> | 饿了么 | `ELEME` | [ElemeAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/ElemeAuthRequest.cs) | [文档](https://open.shop.ele.me/openapi/documents/khd001) |
| <img src="docs/images/platforms/taobao.png" width="18"> | 淘宝 | `TAOBAO` | [TaobaoAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/TaobaoAuthRequest.cs) | [文档](https://open.taobao.com/doc.htm?docId=105590&docType=1) |
| <img src="docs/images/platforms/kujiale.png" width="18"> | 酷家乐 | `KUJIALE` | [KujialeAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/KujialeAuthRequest.cs) | [文档](https://open.kujiale.com/open/apps/2/docs?doc_id=95) |
| <img src="docs/images/platforms/teambition.png" width="18"> | Teambition | `TEAMBITION` | [TeambitionAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/TeambitionAuthRequest.cs) | [文档](https://docs.teambition.com/) |
| <img src="docs/images/platforms/renren.png" width="18"> | 人人网 | `RENREN` | [RenrenAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/RenrenAuthRequest.cs) | [文档](http://open.renren.com/wiki/OAuth2.0) |
| <img src="docs/images/platforms/github.png" width="18"> | GitHub | `GITHUB` | [GithubAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/GithubAuthRequest.cs) | [文档](https://docs.github.com/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps) |
| <img src="docs/images/platforms/gitlab.png" width="18"> | GitLab | `GITLAB` | [GitlabAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/GitlabAuthRequest.cs) | [文档](https://docs.gitlab.com/ee/api/oauth2.html) |
| <img src="docs/images/platforms/google.png" width="18"> | Google | `GOOGLE` | [GoogleAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/GoogleAuthRequest.cs) | [文档](https://developers.google.com/identity/protocols/oauth2/web-server) |
| <img src="docs/images/platforms/microsoft.png" width="18"> | Microsoft | `MICROSOFT` | [MicrosoftAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/MicrosoftAuthRequest.cs) | [文档](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-auth-code-flow) |
| <img src="docs/images/platforms/facebook.png" width="18"> | Facebook | `FACEBOOK` | [FackbookAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/FackbookAuthRequest.cs) | [文档](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow) |
| <img src="docs/images/platforms/linkedin.png" width="18"> | LinkedIn | `LINKEDIN` | [LinkedInAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/LinkedinAuthRequest.cs) | [文档](https://learn.microsoft.com/linkedin/shared/authentication/authorization-code-flow) |
| <img src="docs/images/platforms/twitter.png" width="18"> | Twitter | `TWITTER` | [TwitteAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/TwitteAuthRequest.cs) | [文档](https://developer.x.com/en/docs/authentication/guides/log-in-with-twitter) |
| <img src="docs/images/platforms/pinterest.png" width="18"> | Pinterest | `PINTEREST` | [PinterestAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/PinterestAuthRequest.cs) | [文档](https://developers.pinterest.com/docs/getting-started/authentication/) |
| <img src="docs/images/platforms/stackoverflow.png" width="18"> | Stack Overflow | `STACK_OVERFLOW` | [StackOverflowAuthRequest](Come.CollectiveOAuth/Request/AuthRequests/StackOverflowAuthRequest.cs) | [文档](https://api.stackexchange.com/docs/authentication) |

各平台需要的额外配置、回调参数以及是否支持刷新和撤销授权，见[文档站的平台详情](https://geekoutnet.github.io/CollectiveOAuth/#platforms)。

## 环境要求

| 项目类型 | 要求 |
|---|---|
| ASP.NET Core | .NET 8 及以上 |
| ASP.NET MVC / Web Forms | .NET Framework 4.6.2 及以上 |
| 其他 | 兼容 .NET Standard 2.0 的运行时 |

## 安装

目前以源码方式引用：

```bash
git clone https://github.com/geekoutnet/CollectiveOAuth.git
dotnet add <你的项目>.csproj reference CollectiveOAuth/Come.CollectiveOAuth/Come.CollectiveOAuth.csproj
```

## 快速开始

以 ASP.NET Core 中接入 GitHub 登录为例。

**1. 注册 state 缓存**

```csharp
builder.Services.AddDistributedMemoryCache();   // 多实例部署请换成 Redis 等分布式缓存
builder.Services.AddSingleton<IAuthStateCache, DistributedAuthStateCache>();
```

**2. 跳转到授权页**

```csharp
var config = new ClientConfig
{
    clientId = "你的 Client ID",
    clientSecret = "你的 Client Secret",
    redirectUri = "https://your.site/oauth2/callback/github"
};

var request = new GithubAuthRequest(config, authStateCache);
return Redirect(request.authorize(AuthStateUtils.createState()));
```

**3. 在回调中获取用户信息**

```csharp
public IActionResult Callback(AuthCallback callback)
{
    var request = new GithubAuthRequest(config, authStateCache);
    AuthResponse response = request.login(callback);
    if (!response.ok())
    {
        return BadRequest(response.msg);
    }

    var user = (AuthUser)response.data;   // uuid、nickname、avatar、email、token 等
    return Ok(user.nickname);
}
```

完整的配置项、错误码、超时与代理设置，请阅读[使用文档](https://geekoutnet.github.io/CollectiveOAuth/)。

## 示例项目

| 项目 | 说明 |
|---|---|
| [Come.AspNetCore.Sample](Come.AspNetCore.Sample) | ASP.NET Core（.NET 8），通过依赖注入使用分布式 state 缓存 |
| [Come.Web.Sample](Come.Web.Sample) | ASP.NET MVC 5（.NET Framework 4.6.2） |

两个示例都按 `CollectiveOAuth_{平台名}_{字段}` 的规则从配置文件读取参数。访问 `/OAuth2/Authorization?authSource=GITHUB` 即可跳转授权。

## 从旧版本升级

本版本按 JustAuth 1.16.7 重新对齐了全部平台，以下变化可能影响现有代码：

| 变化 | 说明 |
|---|---|
| 目标框架 | 不再支持 .NET Framework 4.5 和 .NET Core 3.1，最低为 .NET Framework 4.6.2 / .NET 8 |
| 配置校验时机 | 创建请求类时即校验配置，缺少必填项抛出 `AuthException`（5002） |
| 默认超时 | 由 100 秒改为 3 秒，可通过 `ClientConfig.httpConfig` 调整 |
| 默认 state 缓存 | 由 2 小时滑动过期改为 3 分钟绝对过期 |
| 未实现的 refresh / revoke | 抛出 `AuthException`（5001 或 5003），不再返回响应对象 |
| `AuthUser.gender` | 改为可空类型，平台未返回性别时为 `null` |
| scope | 新增 `ClientConfig.scopes` 列表；部分平台的 scope 按 JustAuth 固定，不再读取 `scope` 配置 |
| 用户原始信息 | 新增 `AuthUser.rawUserInfo`（`JObject`），保留 `originalUser` 以兼容旧代码 |

## 与 JustAuth 的差异

在功能行为与 JustAuth 一致的前提下，以下几点是有意保留的不同：

- state 校验通过后立即删除，防止回调被重放。
- 授权地址参数默认 URL 编码。
- 第三方返回非 2xx 时，错误信息中包含状态码和响应内容。
- 提供 `DistributedAuthStateCache`，支持多实例部署。
- 沿用 C# 版原有命名，例如小米的平台名为 `XIAOMI`（JustAuth 为 `MI`）。

## 开发与测试

```bash
dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj   # 编译 net462 / netstandard2.0 / net8.0
dotnet test Come.CollectiveOAuth.Tests                          # 单元测试与差分测试
```

差分测试的原理：[tools/justauth-golden](tools/justauth-golden) 用 JustAuth 1.16.7 执行相同的输入，把授权地址、请求序列和返回结果记录为标准答案（`Come.CollectiveOAuth.Tests/Golden`）；C# 端回放同样的输入，并与标准答案逐项比对。新增或修改平台时，请同时补充 fixture 并重新生成标准答案。

## 路线图

- [x] 对齐 JustAuth 1.16.7 基础设施：配置、异常、缓存、Scope、PKCE
- [x] 31 个已有平台对齐并通过差分测试
- [x] 使用文档站
- [ ] 新增 JustAuth 中的其余 20 个平台：飞书、京东、阿里云、喜马拉雅、Apple、Amazon、Slack、LINE、Okta、钉钉 V2 与钉钉账号登录、华为 V3、企业微信 V2 与第三方扫码、微软中国版、CSDN、程序员客栈、爱发电、微信小程序、QQ 小程序
- [ ] 发布 NuGet 包

## 贡献

欢迎提交 Issue 和 Pull Request：

1. Fork 本仓库并创建分支；
2. 修改代码，并补充对应的测试；
3. 确认 `dotnet test` 全部通过；
4. 向 `dev` 分支发起 Pull Request。

## 致谢

- [JustAuth](https://github.com/justauth/JustAuth)：Java 第三方登录授权 SDK，本项目的设计与行为基准。
- [iconfont](https://www.iconfont.cn)：文档中的平台图标大多来自该平台。
- 感谢 JetBrains 提供的开源 License。

## 许可证

[AGPL-3.0](LICENSE)
