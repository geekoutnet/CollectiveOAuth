# Rthinking.CollectiveOAuth

.NET 平台第三方 OAuth 登录聚合库，对齐 [JustAuth](https://github.com/justauth/JustAuth) 1.16.7。一套接口接入 31 个国内外平台。

- 国内：微信公众号、微信开放平台、企业微信、支付宝、钉钉、QQ、微博、百度、码云、开源中国、Coding、小米、华为、抖音、今日头条、美团、饿了么、淘宝、酷家乐、Teambition、人人网
- 海外：GitHub、GitLab、Google、Microsoft、Facebook、LinkedIn、Twitter、Pinterest、Stack Overflow

支持 .NET Framework 4.6.2+、.NET Standard 2.0 和 .NET 8。

## 安装

```bash
dotnet add package Rthinking.CollectiveOAuth
```

## 使用

```csharp
// ASP.NET Core：注册 state 缓存（多实例部署请换成 Redis）
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IAuthStateCache, DistributedAuthStateCache>();
```

```csharp
var config = new ClientConfig
{
    clientId = "你的 Client ID",
    clientSecret = "你的 Client Secret",
    redirectUri = "https://your.site/oauth2/callback/github"
};

// 跳转到授权页
var request = new GithubAuthRequest(config, authStateCache);
return Redirect(request.authorize(AuthStateUtils.createState()));

// 回调中获取用户信息
AuthResponse response = request.login(callback);
if (response.ok())
{
    var user = (AuthUser)response.data;
}
```

## 文档

- 使用文档：https://geekoutnet.github.io/CollectiveOAuth/
- 源码与问题反馈：https://github.com/geekoutnet/CollectiveOAuth
- 从 1.x 升级：https://github.com/geekoutnet/CollectiveOAuth#从旧版本升级

## 许可证

AGPL-3.0
