<p align="center">
  <img src="docs/images/logo.png" width="360" alt="CollectiveOAuth">
</p>

<p align="center">
  <strong>Third-party OAuth login for .NET</strong><br>
  One API for 31 providers, including WeChat, Alipay, DingTalk, QQ, GitHub, Google and Microsoft
</p>

<p align="center">
  <a href="https://geekoutnet.github.io/CollectiveOAuth/"><img src="https://img.shields.io/badge/docs-online-4b32c3" alt="Docs"></a>
  <img src="https://img.shields.io/badge/.NET%20Framework-4.6.2+-512bd4" alt=".NET Framework 4.6.2+">
  <img src="https://img.shields.io/badge/.NET%20Standard-2.0-512bd4" alt=".NET Standard 2.0">
  <img src="https://img.shields.io/badge/.NET-8.0-512bd4" alt=".NET 8">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-AGPL--3.0-blue" alt="AGPL-3.0"></a>
</p>

<p align="center">
  <a href="https://geekoutnet.github.io/CollectiveOAuth/">Documentation (Chinese)</a> ·
  <a href="README.md">中文</a>
</p>

---

## Overview

CollectiveOAuth hides the protocol differences between OAuth providers. Every provider is used the same way:

```text
your app ──authorize()──▶ provider consent page ──callback──▶ login() ──▶ AuthUser
```

The library is a C# implementation of [JustAuth](https://github.com/justauth/JustAuth) 1.16.7. For every provider, the authorization URL, the outgoing HTTP requests and the parsed user are verified against JustAuth by automated differential tests.

## Features

- **31 providers**: WeChat (Official Account, Open Platform, WeCom), Alipay, DingTalk, QQ, Weibo, Douyin, Huawei, Taobao, GitHub, GitLab, Google, Microsoft, Facebook, Twitter, LinkedIn, Stack Overflow and more.
- **One model for all providers**: `ClientConfig`, `AuthCallback`, `AuthResponse` and `AuthUser` are shared; switching providers means switching the request class.
- **Secure defaults**: `state` is single-use (CSRF and replay protection), and query parameters are URL-encoded.
- **Multi-instance ready**: `DistributedAuthStateCache` works with any `IDistributedCache`, such as Redis.
- **Configurable networking**: per-provider timeout and proxy; HTTP errors include the status code and response body.
- **Targets** .NET Framework 4.6.2+, .NET Standard 2.0 and .NET 8.

## Installation

The library is currently consumed from source:

```bash
git clone https://github.com/geekoutnet/CollectiveOAuth.git
dotnet add <YourProject>.csproj reference CollectiveOAuth/Come.CollectiveOAuth/Come.CollectiveOAuth.csproj
```

## Quick start (ASP.NET Core, GitHub)

```csharp
// Program.cs
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IAuthStateCache, DistributedAuthStateCache>();
```

```csharp
var config = new ClientConfig
{
    clientId = "<client id>",
    clientSecret = "<client secret>",
    redirectUri = "https://your.site/oauth2/callback/github"
};

// Redirect the user to the consent page
var request = new GithubAuthRequest(config, authStateCache);
return Redirect(request.authorize(AuthStateUtils.createState()));

// In the callback action
AuthResponse response = request.login(callback);
if (response.ok())
{
    var user = (AuthUser)response.data;
}
```

## Supported providers

`WECHAT_MP`, `WECHAT_OPEN`, `WECHAT_ENTERPRISE`, `WECHAT_ENTERPRISE_SCAN`, `ALIPAY_MP`, `DINGTALK_SCAN`, `QQ`, `WEIBO`, `BAIDU`, `GITEE`, `OSCHINA`, `CODING`, `XIAOMI`, `HUAWEI`, `DOUYIN`, `TOUTIAO`, `MEITUAN`, `ELEME`, `TAOBAO`, `KUJIALE`, `TEAMBITION`, `RENREN`, `GITHUB`, `GITLAB`, `GOOGLE`, `MICROSOFT`, `FACEBOOK`, `LINKEDIN`, `TWITTER`, `PINTEREST`, `STACK_OVERFLOW`.

See the [Chinese README](README.md#支持的平台) for request classes and links to each provider's documentation.

## Development

```bash
dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj
dotnet test Come.CollectiveOAuth.Tests
```

Golden files in `Come.CollectiveOAuth.Tests/Golden` are produced by running JustAuth 1.16.7 through [tools/justauth-golden](tools/justauth-golden); the C# tests replay the same inputs and compare the results.

## License

[AGPL-3.0](LICENSE)
