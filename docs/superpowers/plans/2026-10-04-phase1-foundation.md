# 阶段 1（地基）+ 1b（差分测试框架）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 CollectiveOAuth 的基础设施（框架、配置、异常、缓存、Scope、PKCE、工具类、HTTP、校验、请求基类）对齐 JustAuth 1.16.7，并搭建 Java↔C# 差分对照测试，用 GitHub 平台跑通整条链路。

**Architecture:** 保留现有 C# 公共 API（`ClientConfig`、`IAuthRequest`、`DefaultAuthRequest`、小写方法名），在其上补齐 JustAuth 的字段与行为。`HttpUtils` 增加可替换的内部发送器 `IHttpSender`，测试时注入回放桩；Java 端通过 simple-http 的 `HttpUtil.setHttp` 注入同样的回放桩，双方输出的「授权 URL / 请求序列 / 结果」经归一化后比对。

**Tech Stack:** C#（net462 / netstandard2.0 / net8.0）、Newtonsoft.Json、xUnit；Java 8、Maven、JustAuth 1.16.7、fastjson 1.2.83。

**参考源码：** JustAuth 已克隆在 `/tmp/JustAuth`（提交 694bbf1）。若丢失：`git clone --depth 1 https://github.com/justauth/JustAuth.git /tmp/JustAuth`。

**通用约定（每个任务都适用）：**
- 构建：`dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj`，期望 `0 个错误`。
- 测试：`dotnet test Come.CollectiveOAuth.Tests`。
- Java 的 `StringUtils.isEmpty` 对应 C# `string.IsNullOrEmpty`（不是 `IsNullOrWhiteSpace`），对齐逻辑时一律用前者。
- Java 的 `TreeMap<String, …>` 对应 C# `new SortedDictionary<string, …>(StringComparer.Ordinal)`。
- **URL 编码规则（有意差异）**：C# `UrlBuilder.build()` 默认编码参数值。移植 Java 代码时，凡是 Java 先手动 `urlEncode`（或 `getScopes(…, true, …)`）再交给 `UrlBuilder.build()` 的值，C# 一律传**未编码**的原值。
- 提交信息结尾加：`Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`

---

## 文件结构

| 文件 | 职责 | 动作 |
|---|---|---|
| `Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` | 目标框架 net462/netstandard2.0/net8.0、依赖、`InternalsVisibleTo` | 改 |
| `Come.CollectiveOAuth.Tests/*` | xUnit 测试项目（单元测试 + 差分测试） | 新建 |
| `Come.CollectiveOAuth/Utils/Base64Utils.cs`、`UuidUtils.cs`、`RandomUtil.cs`、`PkceUtil.cs`、`AuthScopeUtils.cs` | 对应 JustAuth 同名工具 | 新建 |
| `Come.CollectiveOAuth/Utils/GlobalAuthUtil.cs` | 补齐 `GlobalAuthUtils` 的全部方法（Java 语义的 urlEncode、签名算法等） | 改 |
| `Come.CollectiveOAuth/Utils/UrlBuilder.cs` | 对齐 `UrlBuilder`（覆盖同名参数、只编码值） | 改 |
| `Come.CollectiveOAuth/Utils/JsonExtensions.cs` | fastjson `JSONObject` 语义的 JObject 读取方法 | 新建 |
| `Come.CollectiveOAuth/Utils/AuthSourceNames.cs` | C# 平台名 ↔ JustAuth 平台名映射 | 新建 |
| `Come.CollectiveOAuth/Utils/AuthChecker.cs` | 对齐 `AuthChecker` | 改 |
| `Come.CollectiveOAuth/Utils/AuthStateUtils.cs` | state 改用 `UuidUtils` | 改 |
| `Come.CollectiveOAuth/Utils/HttpUtils.cs` | 支持 `HttpConfig`、默认超时 3000ms、默认 UA、可注入 `IHttpSender` | 改 |
| `Come.CollectiveOAuth/Exceptions/AuthException.cs` | 对应 `AuthException` | 新建 |
| `Come.CollectiveOAuth/Enums/AuthResponseStatus.cs` | 补 5011~5016 | 改 |
| `Come.CollectiveOAuth/Enums/Scope/IAuthScope.cs` + 32 个 `Auth*Scope.cs` | Scope 定义（脚本生成） | 新建 |
| `tools/gen_scopes.py` | 从 Java 源码生成 Scope 类 | 新建 |
| `Come.CollectiveOAuth/Config/ClientConfig.cs`、`HttpConfig.cs` | 补齐 `AuthConfig` 字段 | 改/新建 |
| `Come.CollectiveOAuth/Models/AuthCallback.cs`、`AuthToken.cs`、`AuthUser.cs` | 补齐字段 | 改 |
| `Come.CollectiveOAuth/Cache/AuthCacheConfig.cs`、`IAuthCache.cs`、`AuthDefaultCache.cs`、`DefaultAuthStateCache.cs` | 对齐 JustAuth 默认缓存（3 分钟、定时清理） | 新建/改 |
| `Come.CollectiveOAuth/Request/DefaultAuthRequest.cs` | 对齐 `AuthDefaultRequest` | 改 |
| `Come.CollectiveOAuth/Request/AuthRequests/GithubAuthRequest.cs` | 对齐 `AuthGithubRequest`（1b 的验证样本） | 改 |
| `tools/justauth-golden/*` | Java 标准答案生成器 | 新建 |
| `Come.CollectiveOAuth.Tests/Fixtures/*.json`、`Golden/*.json` | 差分输入 / Java 输出 | 新建 |

---

### Task 1: 目标框架升级

**Files:**
- Modify: `Come.CollectiveOAuth/Come.CollectiveOAuth.csproj`
- Modify: `Come.CollectiveOAuth/Cache/HttpRuntimeCache.cs:1`、`Cache/DistributedAuthStateCache.cs:1`、`Utils/AppSettingUtils.cs:6,27`、`Utils/HttpUtils.cs:21`
- Modify: `Come.AspNetCore.Sample/Come.AspNetCore.Sample.csproj`
- Modify: `Come.Web.Sample/Come.Web.Sample.csproj:17`

- [ ] **Step 1: 改写类库 csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net462;netstandard2.0;net8.0</TargetFrameworks>
    <SignAssembly>false</SignAssembly>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="12.0.3" />
    <InternalsVisibleTo Include="Come.CollectiveOAuth.Tests" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="TopSdk">
      <HintPath>Libraries\TopSdk.dll</HintPath>
    </Reference>
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'net462'">
    <PackageReference Include="AlipaySDKNet" Version="4.2.13" />
    <Reference Include="System.Core" />
    <Reference Include="System.Data" />
    <Reference Include="System.Data.DataSetExtensions" />
    <Reference Include="System.Net.Http" />
    <Reference Include="System.Runtime.Serialization" />
    <Reference Include="System.Web" />
    <Reference Include="System.Web.Extensions" />
    <Reference Include="System.Xml" />
    <Reference Include="System.Xml.Linq" />
    <Reference Include="System.Configuration" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' != 'net462'">
    <PackageReference Include="AlipaySDKNet.Standard" Version="4.2.13" />
    <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="3.1.1" />
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="3.1.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="3.1.1" />
    <PackageReference Include="System.Xml.XmlDocument" Version="4.3.0" />
    <PackageReference Include="System.Xml.XPath.XmlDocument" Version="4.3.0" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
    <PackageReference Include="Microsoft.AspNetCore.Http" Version="2.2.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 条件编译符号替换**

`NET45` → `NETFRAMEWORK`（`#if NET45` / `#if !NET45`），`NETCOREAPP3_1` → `NETCOREAPP`：

```bash
cd Come.CollectiveOAuth
sed -i 's/#if NET45/#if NETFRAMEWORK/; s/#if !NET45/#if !NETFRAMEWORK/' Cache/HttpRuntimeCache.cs Cache/DistributedAuthStateCache.cs Utils/AppSettingUtils.cs
sed -i 's/#if NETCOREAPP3_1/#if NETCOREAPP/' Utils/HttpUtils.cs
grep -rn "#if" --include=*.cs . | grep -v /obj/
```
期望：只剩 `NETFRAMEWORK` / `NETCOREAPP`。

- [ ] **Step 3: 示例项目升级**

`Come.AspNetCore.Sample/Come.AspNetCore.Sample.csproj`：`<TargetFramework>netcoreapp3.1</TargetFramework>` → `<TargetFramework>net8.0</TargetFramework>`。
`Come.Web.Sample/Come.Web.Sample.csproj` 第 17 行：`<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>` → `<TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>`。
**不要改 `Come.Web.Sample/Web.config`**（含用户本地密钥的未提交改动）；在阶段汇报中提醒用户把其中 `targetFramework="4.5"` 自行改为 `4.6.2`。

- [ ] **Step 4: 构建验证**

Run: `dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` 和 `dotnet build Come.AspNetCore.Sample/Come.AspNetCore.Sample.csproj`
Expected: 均为 `0 个错误`，输出含 `net462`、`netstandard2.0`、`net8.0` 三个 dll。若 net8.0 缺少 `System.Web.HttpUtility` 之外的类型，按编译错误补对应包，不要引入 `Microsoft.AspNetCore.App`。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth Come.AspNetCore.Sample/Come.AspNetCore.Sample.csproj Come.Web.Sample/Come.Web.Sample.csproj
git commit -m "build: 目标框架升级为 net462/netstandard2.0/net8.0"
```

---

### Task 2: 测试项目骨架

**Files:**
- Create: `Come.CollectiveOAuth.Tests/Come.CollectiveOAuth.Tests.csproj`
- Create: `Come.CollectiveOAuth.Tests/SmokeTest.cs`
- Modify: `Come.Web.Sample.sln`（`dotnet sln add`）

- [ ] **Step 1: 创建 csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RollForward>LatestMajor</RollForward>
    <IsPackable>false</IsPackable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Come.CollectiveOAuth\Come.CollectiveOAuth.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Include="Fixtures\**\*.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="Golden\**\*.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: 冒烟测试**

```csharp
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class SmokeTest
    {
        [Fact]
        public void Library_is_referenced()
        {
            Assert.NotNull(typeof(Come.CollectiveOAuth.Config.ClientConfig));
        }
    }
}
```

- [ ] **Step 3: 加入解决方案并运行**

```bash
dotnet sln Come.Web.Sample.sln add Come.CollectiveOAuth.Tests/Come.CollectiveOAuth.Tests.csproj
dotnet test Come.CollectiveOAuth.Tests
```
Expected: `已通过! - 失败: 0，通过: 1`。若本机无 net8 运行时导致无法启动，确认 `RollForward=LatestMajor` 已生效（`bin/Debug/net8.0/Come.CollectiveOAuth.Tests.runtimeconfig.json` 含 `"rollForward": "LatestMajor"`）。

- [ ] **Step 4: 提交**

```bash
git add Come.CollectiveOAuth.Tests Come.Web.Sample.sln
git commit -m "test: 新增 xUnit 测试项目"
```

---

### Task 3: 基础工具类（Base64 / UUID / 随机串 / PKCE）

**Files:**
- Create: `Come.CollectiveOAuth/Utils/Base64Utils.cs`、`UuidUtils.cs`、`RandomUtil.cs`、`PkceUtil.cs`
- Modify: `Come.CollectiveOAuth/Utils/AuthStateUtils.cs`
- Test: `Come.CollectiveOAuth.Tests/Utils/BasicUtilsTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using System.Text.RegularExpressions;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests.Utils
{
    public class BasicUtilsTest
    {
        [Fact]
        public void Uuid_is_32_lowercase_hex()
        {
            Assert.Matches("^[0-9a-f]{32}$", UuidUtils.getUUID());
            Assert.Matches("^[0-9a-f]{32}$", AuthStateUtils.createState());
        }

        [Fact]
        public void RandomString_uses_base_chars()
        {
            Assert.Matches("^[a-z0-9]{50}$", RandomUtil.randomString(50));
            Assert.Equal(1, RandomUtil.randomString(0).Length);
        }

        [Fact]
        public void Base64_standard_and_urlsafe()
        {
            Assert.Equal("5L2g5aW9", Base64Utils.encode("你好"));
            // 0xfb 0xff → 标准 "+/8="，URL 安全且无填充 "-_8"
            Assert.Equal("+/8=", Base64Utils.encode(new byte[] { 0xfb, 0xff }));
            Assert.Equal("-_8", Base64Utils.encodeUrlSafe(new byte[] { 0xfb, 0xff }));
        }

        [Fact]
        public void Pkce_challenge_matches_rfc7636_example()
        {
            // RFC 7636 附录 B 的示例
            Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                PkceUtil.generateCodeChallenge("S256", "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
            Assert.Equal("plain-verifier", PkceUtil.generateCodeChallenge("plain", "plain-verifier"));
            Assert.Matches("^[A-Za-z0-9_-]+$", PkceUtil.generateCodeVerifier());
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter BasicUtilsTest`
Expected: 编译失败，`UuidUtils`/`RandomUtil`/`Base64Utils`/`PkceUtil` 不存在。

- [ ] **Step 3: 实现**

`Utils/Base64Utils.cs`：
```csharp
using System;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth Base64Utils，URL 安全模式不带填充
    /// </summary>
    public static class Base64Utils
    {
        public static string encode(byte[] source)
        {
            return source == null ? null : Convert.ToBase64String(source);
        }

        public static string encode(string source)
        {
            return source == null ? null : encode(Encoding.UTF8.GetBytes(source));
        }

        public static string encodeUrlSafe(byte[] source)
        {
            return source == null ? null : Convert.ToBase64String(source).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string encodeUrlSafe(string source)
        {
            return source == null ? null : encodeUrlSafe(Encoding.UTF8.GetBytes(source));
        }
    }
}
```

`Utils/RandomUtil.cs`：
```csharp
using System;
using System.Security.Cryptography;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth RandomUtil
    /// </summary>
    public static class RandomUtil
    {
        public const string BASE_CHAR_NUMBER = "abcdefghijklmnopqrstuvwxyz0123456789";

        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        /// <summary>
        /// 返回 [0, bound) 的随机整数
        /// </summary>
        public static int nextInt(int bound)
        {
            var bytes = new byte[4];
            lock (rng)
            {
                rng.GetBytes(bytes);
            }
            return (int)(BitConverter.ToUInt32(bytes, 0) % (uint)bound);
        }

        public static string randomString(int length)
        {
            if (length < 1)
            {
                length = 1;
            }
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(BASE_CHAR_NUMBER[nextInt(BASE_CHAR_NUMBER.Length)]);
            }
            return sb.ToString();
        }
    }
}
```

`Utils/UuidUtils.cs`：
```csharp
using System;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth UuidUtils：32 位小写十六进制
    /// </summary>
    public static class UuidUtils
    {
        public static string getUUID()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
```

`Utils/PkceUtil.cs`：
```csharp
using System;
using System.Security.Cryptography;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth PkceUtil
    /// </summary>
    public static class PkceUtil
    {
        public static string generateCodeVerifier()
        {
            return Base64Utils.encodeUrlSafe(RandomUtil.randomString(50));
        }

        public static string generateCodeChallenge(string codeChallengeMethod, string codeVerifier)
        {
            if ("S256".Equals(codeChallengeMethod, StringComparison.OrdinalIgnoreCase))
            {
                using (var sha256 = SHA256.Create())
                {
                    return Base64Utils.encodeUrlSafe(sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier)));
                }
            }
            return codeVerifier;
        }
    }
}
```

`Utils/AuthStateUtils.cs` 的 `createState()` 方法体改为：
```csharp
            return UuidUtils.getUUID();
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter BasicUtilsTest`
Expected: 4 个测试全部通过。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Utils Come.CollectiveOAuth.Tests/Utils
git commit -m "feat: 新增 Base64/UUID/随机串/PKCE 工具类，state 改为 32 位 UUID"
```

---

### Task 4: GlobalAuthUtil 对齐 GlobalAuthUtils

**Files:**
- Modify: `Come.CollectiveOAuth/Utils/GlobalAuthUtil.cs`
- Test: `Come.CollectiveOAuth.Tests/Utils/GlobalAuthUtilTest.cs`

说明：保留文件中已有的字典扩展方法（`parseObject`、`getString` 等，旧平台代码仍在用）；`urlEncode` 改为 Java 语义（只保留 `A-Za-z0-9.-_`，其余按 UTF-8 编为大写 `%XX`，空格为 `%20`）。

- [ ] **Step 1: 写失败的测试（移植 GlobalAuthUtilsTest）**

```csharp
using System.Collections.Generic;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests.Utils
{
    public class GlobalAuthUtilTest
    {
        private const string TwitterUserInfo = "https://api.twitter.com/1.1/account/verify_credentials.json";
        private const string TwitterAccessToken = "https://api.twitter.com/oauth/access_token";

        [Fact]
        public void generateDingTalkSignature()
        {
            Assert.Equal("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%2FNis5lq9ik%3D", GlobalAuthUtil.generateDingTalkSignature("SHA-256", "1562325753000 "));
        }

        [Fact]
        public void urlDecode()
        {
            Assert.Equal("", GlobalAuthUtil.urlDecode(null));
            Assert.Equal("https://www.foo.bar", GlobalAuthUtil.urlDecode("https://www.foo.bar"));
            Assert.Equal("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe/Nis5lq9ik=", GlobalAuthUtil.urlDecode("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%2FNis5lq9ik%3D"));
        }

        [Fact]
        public void parseStringToMap()
        {
            var map = GlobalAuthUtil.parseStringToMap("foo&bar=baz");
            Assert.Single(map);
            Assert.Equal("baz", map["bar"]);
            Assert.Empty(GlobalAuthUtil.parseStringToMap("bar=baz"));
            Assert.Null(GlobalAuthUtil.parseStringToMap("a=&b=1")["a"]);
        }

        [Fact]
        public void protocols()
        {
            Assert.False(GlobalAuthUtil.isHttpProtocol(""));
            Assert.False(GlobalAuthUtil.isHttpProtocol("foo"));
            Assert.True(GlobalAuthUtil.isHttpProtocol("http://www.foo.bar"));
            Assert.True(GlobalAuthUtil.isHttpProtocol("http%3A%2F%2Fwww.foo.bar"));
            Assert.False(GlobalAuthUtil.isHttpsProtocol(""));
            Assert.True(GlobalAuthUtil.isHttpsProtocol("https://www.foo.bar"));
            Assert.False(GlobalAuthUtil.isLocalHost("foo"));
            Assert.True(GlobalAuthUtil.isLocalHost(""));
            Assert.True(GlobalAuthUtil.isLocalHost("127.0.0.1"));
            Assert.True(GlobalAuthUtil.isLocalHost("localhost"));
        }

        [Fact]
        public void twitterSignatureForRequestToken()
        {
            var p = new Dictionary<string, string>
            {
                { "oauth_consumer_key", "HD0XLqzi5Wz0G08rh45Cg8mgh" },
                { "oauth_nonce", "sTj7Ivg73u052eXstpoS1AWQCynuDEPN" },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", "1569750981" },
                { "oauth_callback", "https://codinglife.tech" },
                { "oauth_version", "1.0" }
            };
            // Java 用例断言整段 Authorization 头，其参数顺序取决于 Java HashMap，这里只比对签名本身
            Assert.Equal("+L5Jq/TaKubge04cWw+4yfjFlaU=", GlobalAuthUtil.generateTwitterSignature(p, "POST", "https://api.twitter.com/oauth/request_token", "0YX3RH2DnPiT77pgzLzFdfpMKX8ENLIWQKYQ7lG5TERuZNgXN5", null));
        }

        [Fact]
        public void twitterSignatureForAccessToken()
        {
            var p = new Dictionary<string, string>
            {
                { "oauth_consumer_key", "HD0XLqzi5Wz0G08rh45Cg8mgh" },
                { "oauth_nonce", "sTj7Ivg73u052eXstpoS1AWQCynuDEPN" },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", "1569751082" },
                { "oauth_token", "W_KLmAAAAAAAxq5LAAABbXxJeD0" },
                { "oauth_verifier", "lYou4gxfA6S5KioUa8VF8HCShzA2nSxp" },
                { "oauth_version", "1.0" }
            };
            Assert.Equal("9i0lmWgvphtkl2KcCO9VyZ3K2/0=", GlobalAuthUtil.generateTwitterSignature(p, "POST", TwitterAccessToken, "0YX3RH2DnPiT77pgzLzFdfpMKX8ENLIWQKYQ7lG5TERuZNgXN5", "W_KLmAAAAAAAxq5LAAABbXxJeD0"));
        }

        [Fact]
        public void twitterSignatureForUserInfo()
        {
            var p = new Dictionary<string, string>
            {
                { "user_id", "1961977975" },
                { "screen_name", "pengisgood" },
                { "include_entities", "true" }
            };
            Assert.Equal("OsqHjRmBf7syxlz8lB7MRdzqEjY=", GlobalAuthUtil.generateTwitterSignature(p, "GET", TwitterUserInfo, "0YX3RH2DnPiT77pgzLzFdfpMKX8ENLIWQKYQ7lG5TERuZNgXN5", "ffyKe39GYYf8tAyhliSe3QmazpO65kZp5b49xOFX6wHho"));
        }

        [Fact]
        public void md5()
        {
            Assert.Equal("b0d923de4289b69976448cac718528b8", GlobalAuthUtil.md5("helloworld,iamjustauth"));
        }

        [Fact]
        public void urlEncode()
        {
            Assert.Equal("", GlobalAuthUtil.urlEncode(null));
            Assert.Equal("https%3A%2F%2Fwww.foo.bar", GlobalAuthUtil.urlEncode("https://www.foo.bar"));
            Assert.Equal("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%252FNis5lq9ik%253D", GlobalAuthUtil.urlEncode("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%2FNis5lq9ik%3D"));
            Assert.Equal("a%20b%2A%7E%21", GlobalAuthUtil.urlEncode("a b*~!"));
        }

        [Fact]
        public void parseMapToString()
        {
            var p = new Dictionary<string, string>
            {
                { "user_id", "1" },
                { "screen_name", "史上最全的第三方授权登录库" },
                { "include_entities", "true" }
            };
            Assert.Equal("user_id=1&screen_name=史上最全的第三方授权登录库&include_entities=true", GlobalAuthUtil.parseMapToString(p, false));
            Assert.Equal("user_id=1&screen_name=%E5%8F%B2%E4%B8%8A%E6%9C%80%E5%85%A8%E7%9A%84%E7%AC%AC%E4%B8%89%E6%96%B9%E6%8E%88%E6%9D%83%E7%99%BB%E5%BD%95%E5%BA%93&include_entities=true", GlobalAuthUtil.parseMapToString(p, true));
            Assert.Equal("", GlobalAuthUtil.parseMapToString((IDictionary<string, string>)null, true));
        }

        [Fact]
        public void nonce_and_timestamp()
        {
            Assert.Equal(10, GlobalAuthUtil.generateNonce(10).Length);
            Assert.Matches("^[0-9]{10}$", GlobalAuthUtil.getTimestamp());
        }

        [Fact]
        public void generateTwitterSignature()
        {
            var p = new Dictionary<string, string> { { "name", "你好" }, { "gender", "male" } };
            Assert.Equal("20FYnV2aZnxNQtp+I0tpMRTvcx0=", GlobalAuthUtil.generateTwitterSignature(p, "GET", TwitterUserInfo, "xxxxx", "xxxxx"));
        }

        [Fact]
        public void generateElemeSignature()
        {
            var p = new Dictionary<string, object> { { "name", "你好" }, { "gender", "male" } };
            Assert.Equal("26FEB8BF7E84FED2619D9C5D97F421BD", GlobalAuthUtil.generateElemeSignature("appKey", "appKey", 1233456789, "appKey", "appKey", p));
        }

        [Fact]
        public void generateJdSignature()
        {
            var p = new Dictionary<string, object> { { "name", "你好" }, { "gender", "male" } };
            Assert.Equal("FE04EC03BA8A619802CF309959C2B43F", GlobalAuthUtil.generateJdSignature("appKey", p));
        }
    }
}
```

> 注：Java 测试里 `TWITTER.userInfo()` 的值以 `/tmp/JustAuth/src/main/java/me/zhyd/oauth/config/AuthDefaultSource.java` 中 `TWITTER` 段为准。执行时先 `grep -n "TWITTER {" -A20` 核对，若不是 `https://api.twitter.com/1.1/account/verify_credentials.json`，同步修改 `TwitterUserInfo` 常量。`generateTwitterSignature` 与 `twitterSignatureForUserInfo` 的期望值依赖该地址。

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter GlobalAuthUtilTest`
Expected: 编译失败（`generateDingTalkSignature` 等不存在）。

- [ ] **Step 3: 实现**

在 `GlobalAuthUtil.cs` 顶部 using 中补充 `System.Globalization`、`System.Security.Cryptography`，并：

1. 把现有 `urlEncode` 方法整体替换为：
```csharp
        /**
         * 编码，与 JustAuth 一致：仅保留 A-Za-z0-9 . - _，其余按 UTF-8 编码为大写 %XX（空格为 %20）
         *
         * @param value str
         * @return encode str
         */
        public static string urlEncode(string value)
        {
            if (value == null)
            {
                return "";
            }
            var builder = new StringBuilder();
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                char c = (char)b;
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('%').Append(b.ToString("X2"));
                }
            }
            return builder.ToString();
        }
```

2. 把 `isHttpProtocol`、`isHttpsProtocol` 替换为（Java 语义：空串判断 + 支持已编码前缀）：
```csharp
        public static bool isHttpProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            return url.StartsWith("http://") || url.StartsWith("http%3A%2F%2F");
        }

        public static bool isHttpsProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            return url.StartsWith("https://") || url.StartsWith("https%3A%2F%2F");
        }
```
`isLocalHost` 中 `string.IsNullOrWhiteSpace` 改为 `string.IsNullOrEmpty`；`getWechatRealGender` 中同样改为 `string.IsNullOrEmpty`。

3. 在类末尾追加：
```csharp
        public static bool isHttpsProtocolOrLocalHost(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            return isHttpsProtocol(url) || isLocalHost(url);
        }

        /// <summary>
        /// 按 Java String.split 语义拆分：去掉末尾的空串
        /// </summary>
        private static string[] javaSplit(string value, char separator)
        {
            var parts = value.Split(separator);
            int len = parts.Length;
            while (len > 0 && parts[len - 1].Length == 0)
            {
                len--;
            }
            var result = new string[len];
            Array.Copy(parts, result, len);
            return result;
        }

        /**
         * string 转 map，格式为 xxx=xxx&xxx=xxx，与 JustAuth parseStringToMap 一致
         */
        public static Dictionary<string, string> parseStringToMap(string accessTokenStr)
        {
            var res = new Dictionary<string, string>();
            if (accessTokenStr.Contains("&"))
            {
                foreach (var field in javaSplit(accessTokenStr, '&'))
                {
                    if (field.Contains("="))
                    {
                        var keyValue = javaSplit(field, '=');
                        res[urlDecode(keyValue[0])] = keyValue.Length == 2 ? urlDecode(keyValue[1]) : null;
                    }
                }
            }
            return res;
        }

        /**
         * map 转 url 参数串，与 JustAuth parseMapToString 一致：仅编码值，null 值输出为 key=
         */
        public static string parseMapToString(IDictionary<string, string> paramMap, bool encode)
        {
            if (paramMap == null || paramMap.Count == 0)
            {
                return "";
            }
            var paramList = new List<string>();
            foreach (var item in paramMap)
            {
                paramList.Add(item.Value == null ? item.Key + "=" : item.Key + "=" + (encode ? urlEncode(item.Value) : item.Value));
            }
            return string.Join("&", paramList);
        }

        public static string generateNonce(int len)
        {
            const string s = "0123456789QWERTYUIOPLKJHGFDSAZXCVBNMqwertyuioplkjhgfdsazxcvbnm";
            var sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                sb.Append(s[RandomUtil.nextInt(62)]);
            }
            return sb.ToString();
        }

        public static string getTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        private static byte[] hmacSha1(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA1(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        private static byte[] hmacSha256(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        public static string generateDingTalkSignature(string secretKey, string timestamp)
        {
            var signData = hmacSha256(Encoding.UTF8.GetBytes(secretKey), Encoding.UTF8.GetBytes(timestamp));
            return urlEncode(Convert.ToBase64String(signData));
        }

        public static string generateTwitterSignature(IDictionary<string, string> paramMap, string method, string baseUrl, string apiSecret, string tokenSecret)
        {
            var map = new SortedDictionary<string, string>(paramMap, StringComparer.Ordinal);
            string str = parseMapToString(map, true);
            string baseStr = method.ToUpper() + "&" + urlEncode(baseUrl) + "&" + urlEncode(str);
            string signKey = apiSecret + "&" + (string.IsNullOrEmpty(tokenSecret) ? "" : tokenSecret);
            return Convert.ToBase64String(hmacSha1(Encoding.UTF8.GetBytes(signKey), Encoding.UTF8.GetBytes(baseStr)));
        }

        public static string generateXmlySignature(IDictionary<string, string> paramMap, string clientSecret)
        {
            var map = new SortedDictionary<string, string>(paramMap, StringComparer.Ordinal);
            string baseStr = Base64Utils.encode(parseMapToString(map, false));
            var sign = hmacSha1(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(baseStr));
            using (var md5Provider = MD5.Create())
            {
                return toHex(md5Provider.ComputeHash(sign));
            }
        }

        public static string generateElemeSignature(string appKey, string secret, long timestamp, string action, string token, IDictionary<string, object> parameters)
        {
            var sorted = new SortedDictionary<string, object>(parameters, StringComparer.Ordinal);
            sorted["app_key"] = appKey;
            sorted["timestamp"] = timestamp;
            var str = new StringBuilder();
            foreach (var entry in sorted)
            {
                str.Append(entry.Key).Append("=").Append(JsonConvert.SerializeObject(entry.Value));
            }
            return md5($"{action}{token}{str}{secret}").ToUpper();
        }

        public static string md5(string str)
        {
            using (var md5Provider = MD5.Create())
            {
                return toHex(md5Provider.ComputeHash(Encoding.UTF8.GetBytes(str)));
            }
        }

        public static string generateJdSignature(string appSecret, IDictionary<string, object> parameters)
        {
            var treeMap = new SortedDictionary<string, object>(parameters, StringComparer.Ordinal);
            var signBuilder = new StringBuilder(appSecret);
            foreach (var entry in treeMap)
            {
                string value = javaValueOf(entry.Value);
                if (!string.IsNullOrEmpty(entry.Key) && !string.IsNullOrEmpty(value))
                {
                    signBuilder.Append(entry.Key).Append(value);
                }
            }
            signBuilder.Append(appSecret);
            return md5(signBuilder.ToString()).ToUpper();
        }

        /// <summary>
        /// 对应 Java String.valueOf(Object)
        /// </summary>
        public static string javaValueOf(object value)
        {
            if (value == null)
            {
                return "null";
            }
            if (value is bool b)
            {
                return b ? "true" : "false";
            }
            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            return value.ToString();
        }

        private static string toHex(byte[] bytes)
        {
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter GlobalAuthUtilTest`
Expected: 全部通过。若 `generateElemeSignature` 不一致，检查 `JsonConvert.SerializeObject` 对中文是否转义（fastjson 不转义，Newtonsoft 默认也不转义）。

- [ ] **Step 5: 构建全部框架并提交**

Run: `dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` → `0 个错误`
```bash
git add Come.CollectiveOAuth/Utils/GlobalAuthUtil.cs Come.CollectiveOAuth.Tests/Utils/GlobalAuthUtilTest.cs
git commit -m "feat: GlobalAuthUtil 对齐 JustAuth（urlEncode 语义与各平台签名算法）"
```

---

### Task 5: UrlBuilder 对齐

**Files:**
- Modify: `Come.CollectiveOAuth/Utils/UrlBuilder.cs`
- Test: `Come.CollectiveOAuth.Tests/Utils/UrlBuilderTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests.Utils
{
    public class UrlBuilderTest
    {
        [Fact]
        public void build_without_encode()
        {
            string url = UrlBuilder.fromBaseUrl("https://www.zhyd.me").queryParam("name", "yadong.zhang").build(false);
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang", url);
            url = UrlBuilder.fromBaseUrl(url).queryParam("github", "https://github.com/zhangyd-c").build(false);
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang&github=https://github.com/zhangyd-c", url);
        }

        [Fact]
        public void build_default_encodes_values_only()
        {
            // 有意差异：JustAuth 的 build() 默认不编码，C# 默认编码
            string url = UrlBuilder.fromBaseUrl("https://www.zhyd.me").queryParam("name", "yadong.zhang").build();
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang", url);
            url = UrlBuilder.fromBaseUrl(url).queryParam("github", "https://github.com/zhangyd-c").build(true);
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang&github=https%3A%2F%2Fgithub.com%2Fzhangyd-c", url);
        }

        [Fact]
        public void same_key_overwrites_and_null_value()
        {
            string url = UrlBuilder.fromBaseUrl("https://a.com").queryParam("k", "1").queryParam("k", "2").queryParam("n", null).build();
            Assert.Equal("https://a.com?k=2&n=", url);
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter UrlBuilderTest`
Expected: `same_key_overwrites_and_null_value` 失败（重复 key 抛 `ArgumentException`）。

- [ ] **Step 3: 实现**

`UrlBuilder.cs` 中：
- 字段改为 `private readonly Dictionary<string, string> paramDic = new Dictionary<string, string>();`
- `queryParam` 方法体改为：
```csharp
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("参数名不能为空");
            }
            this.paramDic[key] = value?.ToString();
            return this;
```
- `build(bool encode)` 方法体改为：
```csharp
            if (this.paramDic.Count == 0)
            {
                return this.baseUrl;
            }
            string baseUrl = this.appendIfNotContain(this.baseUrl, "?", "&");
            return baseUrl + GlobalAuthUtil.parseMapToString(this.paramDic, encode);
```
- 新增：
```csharp
        public IReadOnlyDictionary<string, string> getReadOnlyParams()
        {
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(this.paramDic);
        }
```
- `appendIfNotContain` 中 `string.IsNullOrWhiteSpace` 改为 `string.IsNullOrEmpty`。

- [ ] **Step 4: 运行全部测试**

Run: `dotnet test Come.CollectiveOAuth.Tests`
Expected: 全部通过。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Utils/UrlBuilder.cs Come.CollectiveOAuth.Tests/Utils/UrlBuilderTest.cs
git commit -m "feat: UrlBuilder 对齐 JustAuth（同名参数覆盖、仅编码参数值）"
```

---

### Task 6: 响应状态码与 AuthException

**Files:**
- Modify: `Come.CollectiveOAuth/Enums/AuthResponseStatus.cs`
- Create: `Come.CollectiveOAuth/Exceptions/AuthException.cs`
- Test: `Come.CollectiveOAuth.Tests/AuthExceptionTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using System;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class AuthExceptionTest
    {
        [Fact]
        public void status_with_source_appends_name()
        {
            var e = new AuthException(AuthResponseStatus.ILLEGAL_STATUS, new GithubAuthSource());
            Assert.Equal(5009, e.errorCode);
            Assert.Equal("Illegal state [GITHUB]", e.errorMsg);
            Assert.Equal("Illegal state [GITHUB]", e.Message);
        }

        [Fact]
        public void message_only_is_failure()
        {
            var e = new AuthException("boom");
            Assert.Equal(5000, e.errorCode);
            Assert.Equal("boom", e.errorMsg);
        }

        [Fact]
        public void cause_constructor_has_no_code()
        {
            var e = new AuthException(new InvalidOperationException("x"));
            Assert.Equal(0, e.errorCode);
            Assert.Null(e.errorMsg);
            Assert.Equal("System.InvalidOperationException: x", e.Message);
        }

        [Fact]
        public void new_status_codes()
        {
            Assert.Equal(5016, (int)AuthResponseStatus.ILLEGAL_WECHAT_AGENT_ID);
            Assert.Equal("Invalid token", AuthResponseStatus.ILLEGAL_TOKEN.GetDesc());
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter AuthExceptionTest`
Expected: 编译失败。

- [ ] **Step 3: 实现**

`AuthResponseStatus.cs` 在 `REQUIRED_REFRESH_TOKEN = 5010,` 之后追加：
```csharp
        [Description("Invalid token")]
        ILLEGAL_TOKEN = 5011,

        [Description("Invalid key identifier(kid)")]
        ILLEGAL_KID = 5012,

        [Description("Invalid team id")]
        ILLEGAL_TEAM_ID = 5013,

        [Description("Invalid client id")]
        ILLEGAL_CLIENT_ID = 5014,

        [Description("Invalid client secret")]
        ILLEGAL_CLIENT_SECRET = 5015,

        [Description("Illegal wechat agent id")]
        ILLEGAL_WECHAT_AGENT_ID = 5016,
```

`Exceptions/AuthException.cs`：
```csharp
using System;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Exceptions
{
    /// <summary>
    /// 对应 JustAuth AuthException
    /// </summary>
    public class AuthException : Exception
    {
        public int errorCode { get; }

        public string errorMsg { get; }

        public AuthException(string errorMsg) : this(Convert.ToInt32(AuthResponseStatus.FAILURE), errorMsg)
        {
        }

        public AuthException(string errorMsg, IAuthSource source) : this(Convert.ToInt32(AuthResponseStatus.FAILURE), errorMsg, source)
        {
        }

        public AuthException(int errorCode, string errorMsg) : base(errorMsg)
        {
            this.errorCode = errorCode;
            this.errorMsg = errorMsg;
        }

        public AuthException(AuthResponseStatus status) : this(Convert.ToInt32(status), status.GetDesc())
        {
        }

        public AuthException(int errorCode, string errorMsg, IAuthSource source) : this(errorCode, $"{errorMsg} [{source.getName()}]")
        {
        }

        public AuthException(AuthResponseStatus status, IAuthSource source) : this(Convert.ToInt32(status), status.GetDesc(), source)
        {
        }

        public AuthException(string message, Exception cause) : base(message, cause)
        {
        }

        /// <summary>
        /// 与 Java 的 RuntimeException(Throwable) 一致，消息为 "异常类型: 消息"
        /// </summary>
        public AuthException(Exception cause) : base($"{cause.GetType().FullName}: {cause.Message}", cause)
        {
        }
    }
}
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter AuthExceptionTest`
Expected: 全部通过。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Enums/AuthResponseStatus.cs Come.CollectiveOAuth/Exceptions Come.CollectiveOAuth.Tests/AuthExceptionTest.cs
git commit -m "feat: 新增 AuthException 与 5011~5016 状态码"
```

---

### Task 7: 模型字段补齐

**Files:**
- Modify: `Come.CollectiveOAuth/Models/AuthCallback.cs`、`AuthToken.cs`、`AuthUser.cs`、`AuthResponse.cs`
- Test: `Come.CollectiveOAuth.Tests/ModelsTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using Come.CollectiveOAuth.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class ModelsTest
    {
        [Fact]
        public void callback_code_falls_back_to_auth_code()
        {
            Assert.Equal("ac", new AuthCallback { auth_code = "ac" }.code);
            Assert.Equal("c", new AuthCallback { code = "c", auth_code = "ac" }.code);
            var twitter = new AuthCallback { oauth_token = "t", oauth_verifier = "v" };
            Assert.Equal("t", twitter.oauthToken);
            Assert.Equal("v", twitter.oauthVerifier);
        }

        [Fact]
        public void new_fields_exist()
        {
            var token = new AuthToken { refreshTokenExpireIn = 1, snapshotUser = true, username = "u", corpId = "c", oauthCallbackConfirmed = null };
            var user = new AuthUser { rawUserInfo = new JObject(), snapshotUser = true, token = token };
            Assert.True(user.snapshotUser);
            Assert.Null(user.token.oauthCallbackConfirmed);
            Assert.True(new AuthResponse(2000, null).ok());
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter ModelsTest`
Expected: 编译失败。

- [ ] **Step 3: 实现**

`AuthCallback.cs` 类体替换为（保留原注释风格）：
```csharp
        private string _code;

        /**
         * 访问AuthorizeUrl后回调时带的参数code，为空时取 auth_code（与 JustAuth 一致）
         */
        public string code
        {
            get { return string.IsNullOrEmpty(_code) ? auth_code : _code; }
            set { _code = value; }
        }

        /**
         * 访问AuthorizeUrl后回调时带的参数auth_code，该参数目前只使用于支付宝登录
         */
        public string auth_code { get; set; }

        /**
         * 访问AuthorizeUrl后回调时带的参数state，用于和请求AuthorizeUrl前的state比较，防止CSRF攻击
         */
        public string state { get; set; }

        /**
         * 华为授权登录接受code的参数名
         */
        public string authorization_code { get; set; }

        /**
         * Twitter回调后返回的oauth_token
         */
        public string oauth_token { get; set; }

        /**
         * Twitter回调后返回的oauth_verifier
         */
        public string oauth_verifier { get; set; }

        /**
         * 苹果仅在用户首次授权应用程序时返回此值
         */
        public string user { get; set; }

        /**
         * 苹果错误信息，仅在用户取消授权时返回此值
         */
        public string error { get; set; }

        /**
         * 兼容旧版本，等同于 oauth_token
         */
        [Newtonsoft.Json.JsonIgnore]
        public string oauthToken
        {
            get { return oauth_token; }
            set { oauth_token = value; }
        }

        /**
         * 兼容旧版本，等同于 oauth_verifier
         */
        [Newtonsoft.Json.JsonIgnore]
        public string oauthVerifier
        {
            get { return oauth_verifier; }
            set { oauth_verifier = value; }
        }
```

`AuthToken.cs`：`public bool oauthCallbackConfirmed { get; set; }` 改为 `public bool? oauthCallbackConfirmed { get; set; }`，并在类末尾追加：
```csharp
        public int refreshTokenExpireIn { get; set; }

        /**
         * 微信公众号 - 网页授权的登录时可用，是否为快照页模式虚拟账号
         */
        public bool snapshotUser { get; set; }

        /**
         * 钉钉账号登录
         */
        public string username { get; set; }

        /**
         * 企业微信第三方应用：授权方企业 ID
         */
        public string corpId { get; set; }
```

`AuthUser.cs` 类末尾追加（`using Newtonsoft.Json.Linq;`）：
```csharp
        /**
         * 第三方平台返回的原始用户信息（与 JustAuth rawUserInfo 一致）
         */
        public JObject rawUserInfo { get; set; }

        /**
         * 微信公众号 - 网页授权的登录时可用，是否为快照页模式虚拟账号
         */
        public bool snapshotUser { get; set; }
```

`AuthResponse.cs` 追加无参构造：
```csharp
        public AuthResponse()
        {
        }
```

- [ ] **Step 4: 运行全部测试与构建**

Run: `dotnet test Come.CollectiveOAuth.Tests` → 全部通过；`dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` → `0 个错误`（若有平台代码把 `oauthCallbackConfirmed` 当 `bool` 读，改为 `?? false` 或 `.GetValueOrDefault()`）。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Models Come.CollectiveOAuth.Tests/ModelsTest.cs
git commit -m "feat: AuthCallback/AuthToken/AuthUser 补齐 JustAuth 字段"
```

---

### Task 8: ClientConfig 与 HttpConfig

**Files:**
- Create: `Come.CollectiveOAuth/Config/HttpConfig.cs`
- Modify: `Come.CollectiveOAuth/Config/ClientConfig.cs`
- Test: `Come.CollectiveOAuth.Tests/ClientConfigTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using Come.CollectiveOAuth.Config;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class ClientConfigTest
    {
        [Fact]
        public void defaults_match_justauth()
        {
            var config = new ClientConfig();
            Assert.Equal("default", config.authServerId);
            Assert.Equal("CorpApp", config.loginType);
            Assert.Equal("zh", config.lang);
            Assert.False(config.ignoreCheckState);
            Assert.Null(config.clientOsType);
            config.authServerId = "aus1";
            Assert.Equal("aus1", config.authServerId);
        }

        [Fact]
        public void unionId_string_is_boolean_flag()
        {
            Assert.True(new ClientConfig { unionId = "true" }.isUnionId());
            Assert.True(new ClientConfig { unionId = "True" }.isUnionId());
            Assert.False(new ClientConfig { unionId = "" }.isUnionId());
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter ClientConfigTest`
Expected: 编译失败。

- [ ] **Step 3: 实现**

`Config/HttpConfig.cs`：
```csharp
using System.Net;

namespace Come.CollectiveOAuth.Config
{
    /// <summary>
    /// HTTP 配置，对应 simple-http HttpConfig
    /// </summary>
    public class HttpConfig
    {
        /// <summary>
        /// 超时时间（毫秒），小于等于 0 表示不限制
        /// </summary>
        public int timeout { get; set; }

        /// <summary>
        /// 代理，为空时不使用代理
        /// </summary>
        public IWebProxy proxy { get; set; }
    }
}
```

`ClientConfig.cs` 在 `scope` 属性之后追加：
```csharp
        /**
         * 是否需要申请 unionid（JustAuth 中为 boolean，这里兼容字符串配置："true" 忽略大小写视为开启）
         */
        public bool isUnionId()
        {
            return "true".Equals(unionId?.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }

        /**
         * 企业微信，授权方的网页应用ID
         */
        public string usertype { get; set; }

        /**
         * 域名前缀。使用 Coding 登录和 Okta 登录时，需要传该值
         */
        public string domainPrefix { get; set; }

        /**
         * 针对国外服务可以单独设置代理
         */
        public HttpConfig httpConfig { get; set; }

        /**
         * 忽略校验 state 参数，默认不开启
         */
        public bool ignoreCheckState { get; set; }

        /**
         * 支持自定义授权平台的 scope 内容
         */
        public System.Collections.Generic.List<string> scopes { get; set; }

        /**
         * 设备ID, 设备唯一标识ID（喜马拉雅）
         */
        public string deviceId { get; set; }

        /**
         * 喜马拉雅：客户端操作系统类型，1-iOS系统，2-Android系统，3-Web
         */
        public int? clientOsType { get; set; }

        /**
         * 喜马拉雅：客户端包名
         */
        public string packId { get; set; }

        /**
         * 是否开启 PKCE 模式
         */
        public bool pkce { get; set; }

        private string _authServerId;

        /**
         * Okta 授权服务器的 ID，默认为 default
         */
        public string authServerId
        {
            get { return string.IsNullOrEmpty(_authServerId) ? "default" : _authServerId; }
            set { _authServerId = value; }
        }

        /**
         * 忽略校验 redirectUri 参数，默认不开启
         */
        public bool ignoreCheckRedirectUri { get; set; }

        /**
         * 适配 builder 模式创建 AuthRequest 对象时，Microsoft Entra ID 的 tenantId
         */
        public string tenantId { get; set; }

        /**
         * 苹果开发者账号中的密钥标识符
         */
        public string kid { get; set; }

        /**
         * 苹果开发者账号中的团队ID
         */
        public string teamId { get; set; }

        /**
         * 企业微信扫码登录类型：ServiceApp 服务商登录、CorpApp 企业自建/代开发应用登录
         */
        public string loginType { get; set; } = "CorpApp";

        /**
         * 企业微信扫码登录语言：zh 中文、en 英文
         */
        public string lang { get; set; } = "zh";

        /**
         * 钉钉：控制输出特定类型的组织列表，org_type=management 表示只输出有管理权限的组织
         */
        public string dingTalkOrgType { get; set; }

        /**
         * 钉钉：用于指定用户需要选择的组织
         */
        public string dingTalkCorpId { get; set; }

        /**
         * 钉钉：true 表示专属帐号登录，展示组织代码输入页
         */
        public bool dingTalkExclusiveLogin { get; set; }

        /**
         * 钉钉：开启了专属帐号功能的组织 corpId
         */
        public string dingTalkExclusiveCorpId { get; set; }
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter ClientConfigTest` → 通过。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Config Come.CollectiveOAuth.Tests/ClientConfigTest.cs
git commit -m "feat: ClientConfig 补齐 JustAuth AuthConfig 全部字段，新增 HttpConfig"
```

---

### Task 9: Scope 定义（脚本生成）

**Files:**
- Create: `tools/gen_scopes.py`
- Create: `Come.CollectiveOAuth/Enums/Scope/IAuthScope.cs`、`Come.CollectiveOAuth/Enums/Scope/Auth*Scope.cs`（生成）
- Create: `Come.CollectiveOAuth/Utils/AuthScopeUtils.cs`
- Test: `Come.CollectiveOAuth.Tests/ScopeTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class ScopeTest
    {
        [Fact]
        public void github_default_scopes_match_java()
        {
            // 期望值来自 /tmp/JustAuth AuthGithubScope 中 isDefault=true 的项，按声明顺序
            var defaults = AuthScopeUtils.getDefaultScopes(AuthGithubScope.values());
            Assert.Equal(GeneratedExpectations.GithubDefaultScopes, defaults);
        }

        [Fact]
        public void getScopes_and_empty()
        {
            Assert.Equal(new[] { "user", "gist" }, AuthScopeUtils.getScopes(AuthGithubScope.USER, AuthGithubScope.GIST));
            Assert.Null(AuthScopeUtils.getScopes());
            Assert.Null(AuthScopeUtils.getDefaultScopes(new IAuthScope[0]));
        }
    }
}
```

`GeneratedExpectations` 由生成脚本一并输出到测试项目（见 Step 3），保证期望值直接来自 Java 源码而非手写。

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter ScopeTest` → 编译失败。

- [ ] **Step 3: 编写生成脚本并生成**

`tools/gen_scopes.py`：
```python
"""从 JustAuth 源码生成 C# Scope 定义。用法：python tools/gen_scopes.py /tmp/JustAuth"""
import pathlib
import re
import sys

src_root = pathlib.Path(sys.argv[1]) / "src/main/java/me/zhyd/oauth/enums/scope"
repo = pathlib.Path(__file__).resolve().parent.parent
out_dir = repo / "Come.CollectiveOAuth/Enums/Scope"
test_out = repo / "Come.CollectiveOAuth.Tests/GeneratedExpectations.cs"
out_dir.mkdir(parents=True, exist_ok=True)


def tokenize_args(text):
    """解析枚举常量括号内的参数：字符串字面量（可用 + 拼接）与 true/false"""
    args, i, current = [], 0, None
    while i < len(text):
        c = text[i]
        if c == '"':
            j, buf = i + 1, []
            while text[j] != '"':
                if text[j] == "\\":
                    buf.append(text[j:j + 2])
                    j += 2
                else:
                    buf.append(text[j])
                    j += 1
            current = (current or "") + "".join(buf)
            i = j + 1
        elif c == ",":
            args.append(current)
            current = None
            i += 1
        elif text.startswith("true", i) or text.startswith("false", i):
            current = text.startswith("true", i)
            i += 4 if current else 5
        else:
            i += 1
    args.append(current)
    return args


def parse_constants(body):
    """遍历 NAME(...) 结构，字符串内的括号不计入层级"""
    consts, i = [], 0
    pattern = re.compile(r"\s*([A-Z][A-Z0-9_]*)\s*\(")
    while True:
        m = pattern.search(body, i)
        if not m:
            break
        depth, j, in_str = 1, m.end(), False
        while depth:
            ch = body[j]
            if ch == "\\" and in_str:
                j += 2
                continue
            if ch == '"':
                in_str = not in_str
            elif not in_str and ch == "(":
                depth += 1
            elif not in_str and ch == ")":
                depth -= 1
            j += 1
        consts.append((m.group(1), tokenize_args(body[m.end():j - 1])))
        i = j
    return consts


def cs_string(value):
    return '"' + value.replace("\\\"", "\"").replace("\\", "\\\\").replace("\"", "\\\"") + '"'


expectations = []
for path in sorted(src_root.glob("Auth*Scope.java")):
    if path.stem == "AuthScope":
        continue
    text = path.read_text(encoding="utf-8")
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    text = re.sub(r"(?m)^\s*//.*$", "", text)
    start = text.index("{", text.index("implements AuthScope")) + 1
    end = text.index(";", start)
    while text.count('"', start, end) % 2:
        end = text.index(";", end + 1)
    consts = parse_constants(text[start:end])
    name = path.stem
    lines = [
        "// <auto-generated>由 tools/gen_scopes.py 从 JustAuth 1.16.7 生成，请勿手动修改</auto-generated>",
        "namespace Come.CollectiveOAuth.Enums.Scope",
        "{",
        f"    public sealed class {name} : IAuthScope",
        "    {",
    ]
    for const, args in consts:
        scope, desc, is_default = args[0], args[1], args[2]
        lines.append(f"        public static readonly {name} {const} = new {name}({cs_string(scope)}, {cs_string(desc)}, {'true' if is_default else 'false'});")
    lines += [
        "",
        "        public string scope { get; }",
        "",
        "        public string description { get; }",
        "",
        "        public bool isDefault { get; }",
        "",
        f"        private {name}(string scope, string description, bool isDefault)",
        "        {",
        "            this.scope = scope;",
        "            this.description = description;",
        "            this.isDefault = isDefault;",
        "        }",
        "",
        f"        public static {name}[] values()",
        "        {",
        f"            return new[] {{ {', '.join(c for c, _ in consts)} }};",
        "        }",
        "    }",
        "}",
        "",
    ]
    (out_dir / f"{name}.cs").write_text("\r\n".join(lines), encoding="utf-8")
    defaults = [a[0] for _, a in consts if a[2]]
    expectations.append((name, defaults))

github = dict(expectations)["AuthGithubScope"]
test_lines = [
    "// <auto-generated>由 tools/gen_scopes.py 生成</auto-generated>",
    "namespace Come.CollectiveOAuth.Tests",
    "{",
    "    internal static class GeneratedExpectations",
    "    {",
    "        public static readonly string[] GithubDefaultScopes = { " + ", ".join(cs_string(s) for s in github) + " };",
    "    }",
    "}",
    "",
]
test_out.write_text("\r\n".join(test_lines), encoding="utf-8")
print(f"generated {len(expectations)} scope classes")
```

`Enums/Scope/IAuthScope.cs`：
```csharp
namespace Come.CollectiveOAuth.Enums.Scope
{
    /// <summary>
    /// 对应 JustAuth AuthScope
    /// </summary>
    public interface IAuthScope
    {
        string scope { get; }

        bool isDefault { get; }
    }
}
```

`Utils/AuthScopeUtils.cs`：
```csharp
using System.Collections.Generic;
using System.Linq;
using Come.CollectiveOAuth.Enums.Scope;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth AuthScopeUtils
    /// </summary>
    public static class AuthScopeUtils
    {
        public static List<string> getDefaultScopes(IAuthScope[] scopes)
        {
            if (scopes == null || scopes.Length == 0)
            {
                return null;
            }
            return scopes.Where(s => s.isDefault).Select(s => s.scope).ToList();
        }

        public static List<string> getScopes(params IAuthScope[] scopes)
        {
            if (scopes == null || scopes.Length == 0)
            {
                return null;
            }
            return scopes.Select(s => s.scope).ToList();
        }
    }
}
```

Run:
```bash
python tools/gen_scopes.py /tmp/JustAuth
ls Come.CollectiveOAuth/Enums/Scope | wc -l
```
Expected: 打印 `generated 31 scope classes`（32 个 Java 文件减去 `AuthScope.java` 接口），目录内 32 个文件（含 `IAuthScope.cs`）。若某个 Java 枚举构造参数不是 `(scope, description, isDefault)` 三元组，脚本会在 `args[2]` 处报错——此时打开该 Java 文件，按其实际参数顺序在脚本里为该文件做特殊处理后重跑。

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter ScopeTest` → 通过；`dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` → `0 个错误`。

- [ ] **Step 5: 提交**

```bash
git add tools/gen_scopes.py Come.CollectiveOAuth/Enums/Scope Come.CollectiveOAuth/Utils/AuthScopeUtils.cs Come.CollectiveOAuth.Tests/ScopeTest.cs Come.CollectiveOAuth.Tests/GeneratedExpectations.cs
git commit -m "feat: 由 JustAuth 源码生成 31 个平台的 Scope 定义与 AuthScopeUtils"
```

---

### Task 10: 默认缓存对齐（3 分钟过期 + 定时清理）

**Files:**
- Create: `Come.CollectiveOAuth/Cache/AuthCacheConfig.cs`、`IAuthCache.cs`、`AuthDefaultCache.cs`
- Modify: `Come.CollectiveOAuth/Cache/DefaultAuthStateCache.cs`
- Test: `Come.CollectiveOAuth.Tests/AuthStateCacheTest.cs`

- [ ] **Step 1: 写失败的测试（移植 AuthStateCacheTest，放大时间以适应 Windows 计时精度）**

```csharp
using System.Threading;
using Come.CollectiveOAuth.Cache;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class AuthStateCacheTest
    {
        [Fact]
        public void cache_default_timeout()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("key1", "value");
            Assert.Equal("value", cache.get("key1"));
            Thread.Sleep(40);
            Assert.Equal("value", cache.get("key1"));
        }

        [Fact]
        public void cache_custom_timeout_expires()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("key2", "value", 200);
            Assert.Equal("value", cache.get("key2"));
            Thread.Sleep(100);
            Assert.Equal("value", cache.get("key2"));
            Thread.Sleep(150);
            Assert.Null(cache.get("key2"));
            Assert.False(cache.containsKey("key2"));
        }

        [Fact]
        public void instances_share_storage_and_remove()
        {
            new DefaultAuthStateCache().cache("key3", "v");
            Assert.True(new DefaultAuthStateCache().containsKey("key3"));
            new DefaultAuthStateCache().remove("key3");
            Assert.False(new DefaultAuthStateCache().containsKey("key3"));
        }

        [Fact]
        public void prune_removes_expired()
        {
            var cache = new AuthDefaultCache();
            cache.set("key4", "v", 1);
            Thread.Sleep(20);
            cache.pruneCache();
            Assert.Null(cache.get("key4"));
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter AuthStateCacheTest` → 编译失败（`AuthDefaultCache` 不存在）。

- [ ] **Step 3: 实现**

`Cache/AuthCacheConfig.cs`：
```csharp
namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 对应 JustAuth AuthCacheConfig
    /// </summary>
    public static class AuthCacheConfig
    {
        /// <summary>
        /// 默认缓存过期时间（毫秒）：3 分钟
        /// </summary>
        public static long timeout = 3 * 60 * 1000;

        /// <summary>
        /// 是否开启定时清理过期缓存
        /// </summary>
        public static bool schedulePrune = true;
    }
}
```

`Cache/IAuthCache.cs`：
```csharp
namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 对应 JustAuth AuthCache
    /// </summary>
    public interface IAuthCache
    {
        void set(string key, string value);

        void set(string key, string value, long timeout);

        string get(string key);

        bool containsKey(string key);

        void remove(string key);

        void pruneCache();
    }
}
```

`Cache/AuthDefaultCache.cs`：
```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 对应 JustAuth AuthDefaultCache：进程内缓存，所有实例共享存储，按毫秒绝对过期
    /// </summary>
    public class AuthDefaultCache : IAuthCache
    {
        private static readonly ConcurrentDictionary<string, CacheState> stateCache = new ConcurrentDictionary<string, CacheState>();

        private Timer pruneTimer;

        public AuthDefaultCache()
        {
            if (AuthCacheConfig.schedulePrune)
            {
                this.schedulePrune(AuthCacheConfig.timeout);
            }
        }

        public void set(string key, string value)
        {
            set(key, value, AuthCacheConfig.timeout);
        }

        public void set(string key, string value, long timeout)
        {
            stateCache[key] = new CacheState(value, timeout);
        }

        public string get(string key)
        {
            return stateCache.TryGetValue(key, out var cacheState) && !cacheState.isExpired() ? cacheState.state : null;
        }

        public bool containsKey(string key)
        {
            return stateCache.TryGetValue(key, out var cacheState) && !cacheState.isExpired();
        }

        public void remove(string key)
        {
            stateCache.TryRemove(key, out _);
        }

        public void pruneCache()
        {
            foreach (var item in stateCache)
            {
                if (item.Value.isExpired())
                {
                    stateCache.TryRemove(item.Key, out _);
                }
            }
        }

        public void schedulePrune(long delay)
        {
            pruneTimer = new Timer(_ => pruneCache(), null, delay, delay);
        }

        private class CacheState
        {
            public string state { get; }

            private readonly long expire;

            public CacheState(string state, long expire)
            {
                this.state = state;
                // 实际过期时间等于当前时间加上有效期
                this.expire = currentTimeMillis() + expire;
            }

            public bool isExpired()
            {
                return currentTimeMillis() > this.expire;
            }

            private static long currentTimeMillis()
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
    }
}
```

`Cache/DefaultAuthStateCache.cs` 整体替换为：
```csharp
namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 默认的 state 缓存，对应 JustAuth AuthDefaultStateCache（默认 3 分钟过期）
    /// </summary>
    public class DefaultAuthStateCache : IAuthStateCache
    {
        private static readonly IAuthCache authCache = new AuthDefaultCache();

        public void cache(string key, string value)
        {
            authCache.set(key, value);
        }

        public void cache(string key, string value, long timeout)
        {
            authCache.set(key, value, timeout);
        }

        public string get(string key)
        {
            return authCache.get(key);
        }

        public bool containsKey(string key)
        {
            return authCache.containsKey(key);
        }

        public void remove(string key)
        {
            authCache.remove(key);
        }
    }
}
```
（`HttpRuntimeCache.cs` 保留不动，作为公开类供外部使用。）

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter AuthStateCacheTest` → 4 个通过。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Cache Come.CollectiveOAuth.Tests/AuthStateCacheTest.cs
git commit -m "feat: 默认 state 缓存对齐 JustAuth（3 分钟绝对过期、定时清理）"
```

---

### Task 11: HttpUtils 支持 HttpConfig 与可注入发送器

**Files:**
- Modify: `Come.CollectiveOAuth/Utils/HttpUtils.cs`
- Test: `Come.CollectiveOAuth.Tests/HttpUtilsTest.cs`、`Come.CollectiveOAuth.Tests/Support/RecordingHttpSender.cs`

- [ ] **Step 1: 写测试支撑类与失败的测试**

`Support/RecordingHttpSender.cs`：
```csharp
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Tests.Support
{
    /// <summary>
    /// 记录请求并按顺序回放响应
    /// </summary>
    internal class RecordingHttpSender : IHttpSender
    {
        private readonly Queue<string> responses;

        public List<HttpRequestRecord> requests { get; } = new List<HttpRequestRecord>();

        public RecordingHttpSender(IEnumerable<string> responses)
        {
            this.responses = new Queue<string>(responses);
        }

        public string Send(HttpRequestRecord request, HttpConfig httpConfig)
        {
            requests.Add(request);
            if (responses.Count == 0)
            {
                throw new InvalidOperationException($"No replay response for request #{requests.Count}: {request.method} {request.url}");
            }
            return responses.Dequeue();
        }
    }
}
```

`HttpUtilsTest.cs`：
```csharp
using System.Collections.Generic;
using Come.CollectiveOAuth.Tests.Support;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    [Collection("HttpSender")]
    public class HttpUtilsTest
    {
        [Fact]
        public void records_requests_through_sender()
        {
            var sender = new RecordingHttpSender(new[] { "r1", "r2", "r3" });
            var original = HttpUtils.Sender;
            HttpUtils.Sender = sender;
            try
            {
                Assert.Equal("r1", HttpUtils.RequestGet("https://a.com/x?y=1", new Dictionary<string, object> { { "Authorization", "token t" } }));
                Assert.Equal("r2", HttpUtils.RequestPost("https://a.com/p", "{\"a\":1}"));
                Assert.Equal("r3", HttpUtils.RequestFormPost("https://a.com/f", "a=1&b=2"));
            }
            finally
            {
                HttpUtils.Sender = original;
            }

            Assert.Equal("GET", sender.requests[0].method);
            Assert.Equal("token t", sender.requests[0].headers["Authorization"]);
            Assert.Equal("POST", sender.requests[1].method);
            Assert.Equal("application/json", sender.requests[1].contentType);
            Assert.Equal("{\"a\":1}", sender.requests[1].body);
            Assert.Equal("application/x-www-form-urlencoded", sender.requests[2].contentType);
        }
    }
}
```

另建 `Support/HttpSenderCollection.cs`，让所有替换 `HttpUtils.Sender` 的测试串行执行：
```csharp
using Xunit;

namespace Come.CollectiveOAuth.Tests.Support
{
    [CollectionDefinition("HttpSender", DisableParallelization = true)]
    public class HttpSenderCollection
    {
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter HttpUtilsTest` → 编译失败（`IHttpSender` 不存在）。

- [ ] **Step 3: 实现**

`Utils/HttpUtils.cs` 整体替换为：
```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Come.CollectiveOAuth.Config;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 一次 HTTP 请求的完整描述
    /// </summary>
    public class HttpRequestRecord
    {
        public string method { get; set; }

        public string url { get; set; }

        /// <summary>
        /// 调用方显式传入的请求头（不含 Content-Type、Host）
        /// </summary>
        public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();

        public string body { get; set; }

        /// <summary>
        /// 不含 charset 的媒体类型，GET 请求为 null
        /// </summary>
        public string contentType { get; set; }

        public string charset { get; set; } = "UTF-8";

        /// <summary>
        /// 是否为 RequestJsonGet（附加 Accept: application/json 与 User-Agent: Foo）
        /// </summary>
        public bool acceptJson { get; set; }
    }

    /// <summary>
    /// HTTP 发送器，测试时可替换为回放实现
    /// </summary>
    public interface IHttpSender
    {
        string Send(HttpRequestRecord request, HttpConfig httpConfig);
    }

    public class HttpUtils
    {
        /// <summary>
        /// 与 simple-http 一致的默认超时（毫秒）
        /// </summary>
        public const int DEFAULT_TIMEOUT = 3000;

        /// <summary>
        /// 与 simple-http 一致的默认 User-Agent
        /// </summary>
        public const string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36 simple-http";

        internal static IHttpSender Sender { get; set; } = new DefaultHttpSender();

        /// <summary>
        /// 模拟Form表单post请求
        /// </summary>
        public static string RequestFormPost(string postUrl, string postData = null, Dictionary<string, object> header = null, string charset = null, HttpConfig httpConfig = null)
        {
            return Post(postUrl, postData, header, charset, "application/x-www-form-urlencoded", httpConfig);
        }

        /// <summary>
        /// 普通post请求
        /// </summary>
        public static string RequestPost(string postUrl, string postData = null, Dictionary<string, object> header = null, string charset = null, HttpConfig httpConfig = null)
        {
            return Post(postUrl, postData, header, charset, "application/json", httpConfig);
        }

        /// <summary>
        /// 有一些请求比较特殊，需要标记Accept为application/json
        /// </summary>
        public static string RequestJsonGet(string url, Dictionary<string, object> header = null, HttpConfig httpConfig = null)
        {
            var record = new HttpRequestRecord { method = "GET", url = url, headers = ToHeaders(header), acceptJson = true };
            return Sender.Send(record, httpConfig);
        }

        /// <summary>
        /// 发送Get请求
        /// </summary>
        public static string RequestGet(string url, Dictionary<string, object> header = null, HttpConfig httpConfig = null)
        {
            var record = new HttpRequestRecord { method = "GET", url = url, headers = ToHeaders(header) };
            return Sender.Send(record, httpConfig);
        }

        private static string Post(string postUrl, string postData, Dictionary<string, object> header, string charset, string mediaType, HttpConfig httpConfig)
        {
            var record = new HttpRequestRecord
            {
                method = "POST",
                url = postUrl,
                headers = ToHeaders(header),
                body = postData ?? "",
                contentType = mediaType,
                charset = string.IsNullOrWhiteSpace(charset) ? "UTF-8" : charset
            };
            // 调用方显式指定的 Content-Type 优先
            foreach (var item in header ?? new Dictionary<string, object>())
            {
                if ("CONTENT-TYPE".Equals(item.Key.ToUpper()))
                {
                    record.contentType = Convert.ToString(item.Value).Split(';')[0].Trim();
                }
            }
            return Sender.Send(record, httpConfig);
        }

        private static Dictionary<string, string> ToHeaders(Dictionary<string, object> header)
        {
            var headers = new Dictionary<string, string>();
            foreach (var item in header ?? new Dictionary<string, object>())
            {
                var key = item.Key.ToUpper();
                if (key == "CONTENT-TYPE" || key == "HOST")
                {
                    continue;
                }
                headers[item.Key] = Convert.ToString(item.Value);
            }
            return headers;
        }
    }

    /// <summary>
    /// 基于 HttpClient 的默认发送器
    /// </summary>
    internal class DefaultHttpSender : IHttpSender
    {
        private static readonly HttpClient defaultClient;

        /// <summary>
        /// 按代理复用 HttpClient
        /// </summary>
        private static readonly ConcurrentDictionary<IWebProxy, HttpClient> proxyClients = new ConcurrentDictionary<IWebProxy, HttpClient>();

        static DefaultHttpSender()
        {
            // 在原有协议基础上追加 TLS1.2（.NET Framework 默认不开启），只设置一次，不覆盖全局配置
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            defaultClient = CreateClient(null);
        }

        private static HttpClient CreateClient(IWebProxy proxy)
        {
#if NETCOREAPP
            // 定期重建连接，使 DNS 变更能够生效
            var handler = new SocketsHttpHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                Proxy = proxy,
                UseProxy = proxy != null
            };
#else
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                Proxy = proxy,
                UseProxy = proxy != null
            };
#endif
            // 不共享 Cookie，防止不同用户的请求互相串号；超时由每次请求单独控制
            return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        }

        public string Send(HttpRequestRecord record, HttpConfig httpConfig)
        {
            var client = httpConfig?.proxy == null ? defaultClient : proxyClients.GetOrAdd(httpConfig.proxy, CreateClient);
            int timeout = httpConfig == null ? HttpUtils.DEFAULT_TIMEOUT : httpConfig.timeout;
            var encoding = Encoding.GetEncoding(record.charset);

            using (var request = new HttpRequestMessage(new HttpMethod(record.method), record.url))
            using (var cts = timeout > 0 ? new CancellationTokenSource(timeout) : new CancellationTokenSource())
            {
                if (record.contentType != null)
                {
                    request.Content = new ByteArrayContent(encoding.GetBytes(record.body ?? ""));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"{record.contentType};charset={record.charset.ToLower()}");
                }
                if (record.acceptJson)
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", "Foo");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                }
                else
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", HttpUtils.DEFAULT_USER_AGENT);
                }
                foreach (var item in record.headers)
                {
                    request.Headers.Remove(item.Key);
                    request.Headers.TryAddWithoutValidation(item.Key, item.Value);
                }

                using (var response = client.SendAsync(request, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult())
                {
                    var bytes = response.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var content = encoding.GetString(bytes);
                    if (!response.IsSuccessStatusCode)
                    {
                        // 响应可能是整页 HTML，截断后再放入异常信息
                        var detail = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
                        throw new Exception($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
                    }
                    return content;
                }
            }
        }
    }
}
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter HttpUtilsTest` → 通过；`dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` → `0 个错误`。

- [ ] **Step 5: 端到端冒烟（真实网络）**

用 Core 示例验证：按上一轮方法以 `DOTNET_ROLL_FORWARD=Major` 启动 `Come.AspNetCore.Sample`，GitHub 回调假 code，期望返回 `{"code":5000,"msg":"HTTP 404 ..."}`（说明真实 HTTP 通路正常）。验证后结束进程。

- [ ] **Step 6: 提交**

```bash
git add Come.CollectiveOAuth/Utils/HttpUtils.cs Come.CollectiveOAuth.Tests
git commit -m "feat: HttpUtils 支持 HttpConfig（超时/代理），默认超时与 UA 对齐 simple-http，发送器可注入"
```

---

### Task 12: 平台名映射与 AuthChecker 对齐

**Files:**
- Create: `Come.CollectiveOAuth/Utils/AuthSourceNames.cs`
- Modify: `Come.CollectiveOAuth/Utils/AuthChecker.cs`
- Test: `Come.CollectiveOAuth.Tests/AuthCheckerTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class AuthCheckerTest
    {
        [Fact]
        public void names_map_both_ways()
        {
            Assert.Equal("MI", AuthSourceNames.toJustAuthName("XIAOMI"));
            Assert.Equal("WECHAT_ENTERPRISE_WEB", AuthSourceNames.toJustAuthName("WECHAT_ENTERPRISE"));
            Assert.Equal("WECHAT_ENTERPRISE", AuthSourceNames.toJustAuthName("WECHAT_ENTERPRISE_SCAN"));
            Assert.Equal("GITHUB", AuthSourceNames.toJustAuthName("GITHUB"));
            Assert.Equal("WECHAT_ENTERPRISE_SCAN", AuthSourceNames.fromJustAuthName("WECHAT_ENTERPRISE"));
            Assert.Equal("XIAOMI", AuthSourceNames.fromJustAuthName("MI"));
        }

        [Fact]
        public void isSupportedAuth_rules()
        {
            Assert.True(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new GithubAuthSource()));
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a" }, new GithubAuthSource()));
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new StackOverflowAuthSource()));
            // C# 的 WECHAT_ENTERPRISE_SCAN 对应 JustAuth 的 WECHAT_ENTERPRISE，需要 agentId
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new WechatEnterpriseScanAuthSource()));
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new CodingAuthSource()));
        }

        [Fact]
        public void checkConfig_rules()
        {
            var ex = Assert.Throws<AuthException>(() => AuthChecker.checkConfig(new ClientConfig(), new GithubAuthSource()));
            Assert.Equal(5006, ex.errorCode);
            Assert.Throws<AuthException>(() => AuthChecker.checkConfig(new ClientConfig { redirectUri = "ftp://a" }, new GithubAuthSource()));
            AuthChecker.checkConfig(new ClientConfig { ignoreCheckRedirectUri = true }, new GithubAuthSource());
            AuthChecker.checkConfig(new ClientConfig { redirectUri = "http://localhost/cb" }, new GithubAuthSource());
        }

        [Fact]
        public void checkCode_rules()
        {
            var ex = Assert.Throws<AuthException>(() => AuthChecker.checkCode(new GithubAuthSource(), new AuthCallback()));
            Assert.Equal("Illegal code [GITHUB]", ex.errorMsg);
            AuthChecker.checkCode(new GithubAuthSource(), new AuthCallback { auth_code = "x" });
            AuthChecker.checkCode(new HuaweiAuthSource(), new AuthCallback { authorization_code = "x" });
        }

        [Fact]
        public void checkState_rules()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("s-ok", "s-ok");
            AuthChecker.checkState("s-ok", new GithubAuthSource(), cache);
            var ex = Assert.Throws<AuthException>(() => AuthChecker.checkState("nope", new GithubAuthSource(), cache));
            Assert.Equal(5009, ex.errorCode);
        }
    }
}
```

> 执行前用 `ls Come.CollectiveOAuth/Config/AuthSources` 核对类名（如 `WechatEnterpriseScanAuthSource`、`StackOverflowAuthSource`、`CodingAuthSource`、`HuaweiAuthSource`），以实际类名为准。

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter AuthCheckerTest` → 编译失败。

- [ ] **Step 3: 实现**

`Utils/AuthSourceNames.cs`：
```csharp
using System.Collections.Generic;
using System.Linq;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// C# 平台名与 JustAuth 平台名的映射（仅列出不同的部分）
    /// </summary>
    public static class AuthSourceNames
    {
        private static readonly Dictionary<string, string> csharpToJustAuth = new Dictionary<string, string>
        {
            { "XIAOMI", "MI" },
            { "ALIPAY_MP", "ALIPAY" },
            { "DINGTALK_SCAN", "DINGTALK" },
            { "WECHAT_ENTERPRISE", "WECHAT_ENTERPRISE_WEB" },
            { "WECHAT_ENTERPRISE_SCAN", "WECHAT_ENTERPRISE" }
        };

        private static readonly Dictionary<string, string> justAuthToCsharp = csharpToJustAuth.ToDictionary(kv => kv.Value, kv => kv.Key);

        public static string toJustAuthName(string name)
        {
            return name != null && csharpToJustAuth.TryGetValue(name, out var value) ? value : name;
        }

        public static string fromJustAuthName(string name)
        {
            return name != null && justAuthToCsharp.TryGetValue(name, out var value) ? value : name;
        }
    }
}
```

`Utils/AuthChecker.cs` 类体整体替换为：
```csharp
    public class AuthChecker
    {
        private static string nameOf(IAuthSource source)
        {
            return AuthSourceNames.toJustAuthName(source.getName().ToUpper());
        }

        /**
         * 是否支持第三方登录
         */
        public static bool isSupportedAuth(ClientConfig config, IAuthSource source)
        {
            string name = nameOf(source);
            bool isSupported = !string.IsNullOrEmpty(config.clientId) && !string.IsNullOrEmpty(config.clientSecret);
            if (isSupported && "STACK_OVERFLOW" == name)
            {
                isSupported = !string.IsNullOrEmpty(config.stackOverflowKey);
            }
            if (isSupported && "WECHAT_ENTERPRISE" == name)
            {
                isSupported = !string.IsNullOrEmpty(config.agentId);
            }
            if (isSupported && ("CODING" == name || "OKTA" == name))
            {
                isSupported = !string.IsNullOrEmpty(config.domainPrefix);
            }
            if (isSupported && "XMLY" == name)
            {
                isSupported = !string.IsNullOrEmpty(config.deviceId) && config.clientOsType != null;
                if (isSupported)
                {
                    isSupported = config.clientOsType == 3 || !string.IsNullOrEmpty(config.packId);
                }
            }
            return isSupported;
        }

        /**
         * 检查配置合法性
         */
        public static void checkConfig(ClientConfig config, IAuthSource source)
        {
            string redirectUri = config.redirectUri;
            if (config.ignoreCheckRedirectUri)
            {
                return;
            }
            if (string.IsNullOrEmpty(redirectUri))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
            if (!GlobalAuthUtil.isHttpProtocol(redirectUri) && !GlobalAuthUtil.isHttpsProtocol(redirectUri))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
        }

        /**
         * 校验回调传回的code
         */
        public static void checkCode(IAuthSource source, AuthCallback callback)
        {
            string name = nameOf(source);
            // 推特平台不支持回调 code 和 state
            if ("TWITTER" == name)
            {
                return;
            }
            string code = callback.code;
            if (string.IsNullOrEmpty(code) && "HUAWEI" == name)
            {
                code = callback.authorization_code;
            }
            if (string.IsNullOrEmpty(code))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_CODE, source);
            }
        }

        /**
         * 校验回调传回的state，为空或者不存在
         */
        public static void checkState(string state, IAuthSource source, IAuthStateCache authStateCache)
        {
            // 推特平台不支持回调 code 和 state
            if ("TWITTER" == nameOf(source))
            {
                return;
            }
            if (string.IsNullOrEmpty(state) || !authStateCache.containsKey(state))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_STATUS, source);
            }
        }
    }
```
using 补 `Come.CollectiveOAuth.Exceptions`，删除不再使用的 using（`System.Linq` 等）。

- [ ] **Step 4: 运行全部测试与构建**

Run: `dotnet test Come.CollectiveOAuth.Tests` → 全部通过；构建 `0 个错误`。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Utils Come.CollectiveOAuth.Tests/AuthCheckerTest.cs
git commit -m "feat: AuthChecker 对齐 JustAuth，新增 C#/JustAuth 平台名映射"
```

---

### Task 13: fastjson 语义的 JSON 读取

**Files:**
- Create: `Come.CollectiveOAuth/Utils/JsonExtensions.cs`
- Test: `Come.CollectiveOAuth.Tests/JsonExtensionsTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class JsonExtensionsTest
    {
        private const string Json = "{\"s\":\"x\",\"n\":12,\"f\":1.50,\"b\":true,\"bs\":\"1\",\"nul\":null,\"d\":\"2020-01-01T00:00:00Z\",\"o\":{\"k\":1},\"a\":[1,2],\"ns\":\"7\"}";

        [Fact]
        public void getString_semantics()
        {
            var o = JsonExtensions.parseObject(Json);
            Assert.Equal("x", o.getString("s"));
            Assert.Equal("12", o.getString("n"));
            Assert.Equal("1.50", o.getString("f"));
            Assert.Equal("true", o.getString("b"));
            Assert.Null(o.getString("nul"));
            Assert.Null(o.getString("missing"));
            Assert.Equal("2020-01-01T00:00:00Z", o.getString("d"));
            Assert.Equal("{\"k\":1}", o.getString("o"));
            Assert.Equal("[1,2]", o.getString("a"));
        }

        [Fact]
        public void numeric_and_boolean_semantics()
        {
            var o = JsonExtensions.parseObject(Json);
            Assert.Equal(12, o.getIntValue("n"));
            Assert.Equal(7, o.getIntValue("ns"));
            Assert.Equal(0, o.getIntValue("missing"));
            Assert.Null(o.getInteger("missing"));
            Assert.Equal(12L, o.getLongValue("n"));
            Assert.True(o.getBooleanValue("b"));
            Assert.True(o.getBooleanValue("bs"));
            Assert.False(o.getBooleanValue("missing"));
            Assert.Null(o.getBoolean("missing"));
            Assert.Equal(1, o.getJSONObject("o").getIntValue("k"));
            Assert.Equal(2, o.getJSONArray("a").Count);
            Assert.True(o.containsKey("nul"));
            Assert.False(o.containsKey("missing"));
        }

        [Fact]
        public void parse_empty_returns_null()
        {
            Assert.Null(JsonExtensions.parseObject(""));
            Assert.Null(JsonExtensions.parseObject(null));
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter JsonExtensionsTest` → 编译失败。

- [ ] **Step 3: 实现**

`Utils/JsonExtensions.cs`：
```csharp
using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 以 fastjson JSONObject 的语义读取 JObject，供对齐 JustAuth 的平台代码使用
    /// </summary>
    public static class JsonExtensions
    {
        private static JToken parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal })
            {
                return JToken.ReadFrom(reader);
            }
        }

        /// <summary>
        /// 对应 JSONObject.parseObject：空串返回 null，非法 JSON 抛出异常
        /// </summary>
        public static JObject parseObject(string json)
        {
            return (JObject)parse(json);
        }

        /// <summary>
        /// 对应 JSONArray.parseArray
        /// </summary>
        public static JArray parseArray(string json)
        {
            return (JArray)parse(json);
        }

        private static JToken valueOf(JObject obj, string key)
        {
            var token = obj?[key];
            return token == null || token.Type == JTokenType.Null ? null : token;
        }

        public static bool containsKey(this JObject obj, string key)
        {
            return obj != null && obj.ContainsKey(key);
        }

        public static string getString(this JObject obj, string key)
        {
            var token = valueOf(obj, key);
            if (token == null)
            {
                return null;
            }
            switch (token.Type)
            {
                case JTokenType.String:
                    return (string)token;
                case JTokenType.Boolean:
                    return (bool)token ? "true" : "false";
                case JTokenType.Integer:
                case JTokenType.Float:
                    return ((JValue)token).ToString(CultureInfo.InvariantCulture);
                default:
                    return token.ToString(Formatting.None);
            }
        }

        public static int? getInteger(this JObject obj, string key)
        {
            var value = obj.getString(key);
            if (string.IsNullOrEmpty(value) || "null".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (value == "true" || value == "false")
            {
                return value == "true" ? 1 : 0;
            }
            return (int)decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static int getIntValue(this JObject obj, string key)
        {
            return obj.getInteger(key) ?? 0;
        }

        public static long? getLong(this JObject obj, string key)
        {
            var value = obj.getString(key);
            if (string.IsNullOrEmpty(value) || "null".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (value == "true" || value == "false")
            {
                return value == "true" ? 1 : 0;
            }
            return (long)decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static long getLongValue(this JObject obj, string key)
        {
            return obj.getLong(key) ?? 0L;
        }

        /// <summary>
        /// fastjson castToBoolean：true/1/Y/T 为真，false/0/N/F 为假，数字非 1 为假
        /// </summary>
        public static bool? getBoolean(this JObject obj, string key)
        {
            var value = obj.getString(key);
            if (string.IsNullOrEmpty(value) || "null".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if ("true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "1" || "Y".Equals(value, StringComparison.OrdinalIgnoreCase) || "T".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if ("false".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "0" || "N".Equals(value, StringComparison.OrdinalIgnoreCase) || "F".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number == 1;
            }
            throw new FormatException($"can not cast to boolean, value : {value}");
        }

        public static bool getBooleanValue(this JObject obj, string key)
        {
            return obj.getBoolean(key) ?? false;
        }

        public static JObject getJSONObject(this JObject obj, string key)
        {
            var token = valueOf(obj, key);
            if (token == null)
            {
                return null;
            }
            return token.Type == JTokenType.String ? parseObject((string)token) : (JObject)token;
        }

        public static JArray getJSONArray(this JObject obj, string key)
        {
            var token = valueOf(obj, key);
            if (token == null)
            {
                return null;
            }
            return token.Type == JTokenType.String ? parseArray((string)token) : (JArray)token;
        }
    }
}
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter JsonExtensionsTest` → 通过。若 `getString("f")` 得到 `1.5`，说明 `FloatParseHandling.Decimal` 未生效，检查是否经由 `parse` 解析。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth/Utils/JsonExtensions.cs Come.CollectiveOAuth.Tests/JsonExtensionsTest.cs
git commit -m "feat: 新增 fastjson 语义的 JObject 读取扩展"
```

---

### Task 14: DefaultAuthRequest 对齐 AuthDefaultRequest

**Files:**
- Modify: `Come.CollectiveOAuth/Request/DefaultAuthRequest.cs`
- Test: `Come.CollectiveOAuth.Tests/DefaultAuthRequestTest.cs`

- [ ] **Step 1: 写失败的测试**

```csharp
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Request;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class DefaultAuthRequestTest
    {
        private class FakeRequest : DefaultAuthRequest
        {
            public Exception toThrow;

            public FakeRequest(ClientConfig config, IAuthStateCache cache) : base(config, new GithubAuthSource(), cache)
            {
            }

            protected override AuthToken getAccessToken(AuthCallback authCallback)
            {
                if (toThrow != null)
                {
                    throw toThrow;
                }
                return new AuthToken { accessToken = "t" };
            }

            protected override AuthUser getUserInfo(AuthToken authToken)
            {
                return new AuthUser { uuid = "u", token = authToken };
            }

            public string scopes(string separator, bool encode, List<string> defaults)
            {
                return getScopes(separator, encode, defaults);
            }
        }

        private static ClientConfig Config()
        {
            return new ClientConfig { clientId = "id", clientSecret = "secret", redirectUri = "https://a.com/cb" };
        }

        [Fact]
        public void constructor_validates_config()
        {
            var ex = Assert.Throws<AuthException>(() => new FakeRequest(new ClientConfig { clientId = "id" }, new DefaultAuthStateCache()));
            Assert.Equal(5002, ex.errorCode);
            Assert.Equal("Parameter incomplete [GITHUB]", ex.errorMsg);
            Assert.Throws<AuthException>(() => new FakeRequest(new ClientConfig { clientId = "id", clientSecret = "s" }, new DefaultAuthStateCache()));
        }

        [Fact]
        public void login_success_consumes_state()
        {
            var cache = new DefaultAuthStateCache();
            var request = new FakeRequest(Config(), cache);
            request.authorize("st-1");
            var response = request.login(new AuthCallback { code = "c", state = "st-1" });
            Assert.Equal(2000, response.code);
            Assert.False(cache.containsKey("st-1"));
            Assert.Equal(5009, request.login(new AuthCallback { code = "c", state = "st-1" }).code);
        }

        [Fact]
        public void login_ignore_check_state()
        {
            var config = Config();
            config.ignoreCheckState = true;
            Assert.Equal(2000, new FakeRequest(config, new DefaultAuthStateCache()).login(new AuthCallback { code = "c" }).code);
        }

        [Fact]
        public void login_error_mapping()
        {
            var cache = new DefaultAuthStateCache();
            var request = new FakeRequest(Config(), cache);
            request.authorize("st-2");
            request.toThrow = new AuthException(5011, "bad token");
            var response = request.login(new AuthCallback { code = "c", state = "st-2" });
            Assert.Equal(5011, response.code);
            Assert.Equal("bad token", response.msg);

            request.authorize("st-3");
            request.toThrow = new InvalidOperationException("plain");
            response = request.login(new AuthCallback { code = "c", state = "st-3" });
            Assert.Equal(5000, response.code);
            Assert.Equal("plain", response.msg);
        }

        [Fact]
        public void getScopes_rules()
        {
            var config = Config();
            var request = new FakeRequest(config, new DefaultAuthStateCache());
            Assert.Equal("", request.scopes(" ", false, null));
            Assert.Equal("a b", request.scopes(null, false, new List<string> { "a", "b" }));
            Assert.Equal("a%20b", request.scopes(" ", true, new List<string> { "a", "b" }));
            config.scopes = new List<string> { "x", "y" };
            Assert.Equal("x,y", request.scopes(",", false, new List<string> { "a" }));
            config.scopes = null;
            config.scope = "legacy scope";
            Assert.Equal("legacy scope", request.scopes(",", false, new List<string> { "a" }));
        }

        [Fact]
        public void refresh_and_revoke_not_implemented_throw()
        {
            var request = new FakeRequest(Config(), new DefaultAuthStateCache());
            Assert.Equal(5001, Assert.Throws<AuthException>(() => request.refresh(new AuthToken())).errorCode);
            Assert.Equal(5001, Assert.Throws<AuthException>(() => request.revoke(new AuthToken())).errorCode);
        }

        [Fact]
        public void authorize_generates_uuid_state()
        {
            var url = new FakeRequest(Config(), new DefaultAuthStateCache()).authorize();
            Assert.Matches("state=[0-9a-f]{32}$", url);
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter DefaultAuthRequestTest` → 编译失败（`getScopes` 不存在）或断言失败。

- [ ] **Step 3: 实现**

`DefaultAuthRequest.cs` 中修改如下（其余 `xxxUrl` 方法保持不变）：

1. using 补 `System.Collections.Generic`、`Come.CollectiveOAuth.Exceptions`。
2. 构造函数：
```csharp
        public DefaultAuthRequest(ClientConfig config, IAuthSource source) : this(config, source, new DefaultAuthStateCache())
        {
        }

        public DefaultAuthRequest(ClientConfig config, IAuthSource source, IAuthStateCache authStateCache)
        {
            this.config = config;
            this.source = source;
            this.authStateCache = authStateCache;
            if (!AuthChecker.isSupportedAuth(config, source))
            {
                throw new AuthException(AuthResponseStatus.PARAMETER_INCOMPLETE, source);
            }
            // 校验配置合法性
            this.checkConfig(config);
        }
```
3. `refresh` / `revoke` 方法体改为 `throw new AuthException(AuthResponseStatus.NOT_IMPLEMENTED);`
4. `login` 整体替换为：
```csharp
        public virtual AuthResponse login(AuthCallback authCallback)
        {
            try
            {
                checkCode(authCallback);
                if (!config.ignoreCheckState)
                {
                    AuthChecker.checkState(authCallback.state, source, authStateCache);
                    // 有意差异：state 一次性使用，校验通过立即删除，防止重放
                    if (!string.IsNullOrEmpty(authCallback.state))
                    {
                        authStateCache.remove(authCallback.state);
                    }
                }

                AuthToken authToken = this.getAccessToken(authCallback);
                AuthUser user = this.getUserInfo(authToken);
                return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, user);
            }
            catch (Exception e)
            {
                return this.responseError(e);
            }
        }

        protected virtual void checkCode(AuthCallback authCallback)
        {
            AuthChecker.checkCode(source, authCallback);
        }

        protected virtual void checkConfig(ClientConfig config)
        {
            AuthChecker.checkConfig(config, source);
        }
```
5. `responseError` 替换为：
```csharp
        protected AuthResponse responseError(Exception e)
        {
            int errorCode = Convert.ToInt32(AuthResponseStatus.FAILURE);
            string errorMsg = e.Message;
            if (e is AuthException authException)
            {
                errorCode = authException.errorCode;
                if (!string.IsNullOrEmpty(authException.errorMsg))
                {
                    errorMsg = authException.errorMsg;
                }
            }
            return new AuthResponse(errorCode, errorMsg);
        }
```
6. `getRealState` 中 `Guid.NewGuid().ToString()` 改为 `UuidUtils.getUUID()`，`string.IsNullOrWhiteSpace` 改为 `string.IsNullOrEmpty`。
7. 新增 `getScopes`：
```csharp
        /**
         * 获取以 separator 分割过后的 scope 信息，与 JustAuth 一致；
         * 兼容旧配置：scopes 为空且配置了 scope 字符串时，直接使用该字符串
         */
        protected string getScopes(string separator, bool encode, List<string> defaultScopes)
        {
            List<string> scopes = config.scopes;
            if (scopes == null || scopes.Count == 0)
            {
                if (!string.IsNullOrEmpty(config.scope))
                {
                    return encode ? GlobalAuthUtil.urlEncode(config.scope) : config.scope;
                }
                if (defaultScopes == null || defaultScopes.Count == 0)
                {
                    return "";
                }
                scopes = defaultScopes;
            }
            if (separator == null)
            {
                // 默认为空格
                separator = " ";
            }
            string scopeStr = string.Join(separator, scopes);
            return encode ? GlobalAuthUtil.urlEncode(scopeStr) : scopeStr;
        }
```
8. 六个 `doXxx` 方法给 `HttpUtils` 调用补上 `httpConfig: config.httpConfig` 参数，例如：
```csharp
            return HttpUtils.RequestPost(accessTokenUrl(code), httpConfig: config.httpConfig);
```

- [ ] **Step 4: 修复平台类编译**

Run: `dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj`
若有平台类调用了被删除/改签名的成员（如旧 `AuthChecker` 重载），按新签名最小修改；**不要**在本任务对齐平台逻辑（属于阶段 2）。

- [ ] **Step 5: 运行全部测试**

Run: `dotnet test Come.CollectiveOAuth.Tests` → 全部通过。

- [ ] **Step 6: 示例项目适配并提交**

`Come.AspNetCore.Sample/Controllers/OAuth2Controller.cs` 与 `Come.Web.Sample/Controllers/OAuth2Controller.cs` 已对 `getRequest` 做了 try/catch，构造期抛出的 `AuthException` 会以 JSON 返回，无需改动；构建 Core 示例确认 `0 个错误`。
```bash
git add Come.CollectiveOAuth/Request/DefaultAuthRequest.cs Come.CollectiveOAuth.Tests/DefaultAuthRequestTest.cs Come.CollectiveOAuth
git commit -m "feat: DefaultAuthRequest 对齐 JustAuth（构造校验、ignoreCheckState、getScopes、错误码映射）"
```

---

### Task 15（阶段 1b）: Java 标准答案生成器

**Files:**
- Create: `tools/justauth-golden/pom.xml`
- Create: `tools/justauth-golden/src/main/java/golden/ReplayHttp.java`
- Create: `tools/justauth-golden/src/main/java/golden/GoldenGenerator.java`
- Create: `tools/justauth-golden/README.md`

**Fixture 格式**（`Come.CollectiveOAuth.Tests/Fixtures/<JustAuth平台名>.json`）：
```json
{
  "source": "GITHUB",
  "config": { "clientId": "cid", "clientSecret": "csecret", "redirectUri": "https://www.justauth.cn/callback/github" },
  "state": "fixedstate000000000000000000000",
  "scenarios": [
    {
      "name": "login_success",
      "action": "login",
      "callback": { "code": "code123", "state": "fixedstate000000000000000000000" },
      "responses": [ "access_token=gho_abc&scope=user&token_type=bearer", "{\"id\":1,\"login\":\"octo\"}" ]
    }
  ]
}
```
- `action`：`login` / `refresh` / `revoke`；`refresh`/`revoke` 场景用 `"token": { ...AuthToken 字段... }` 代替 `callback`。
- `redirectUri` 不得包含 `?`、`&`（Java 版不编码，含这些字符会让双方 URL 无法比较）。

**Golden 格式**（`Come.CollectiveOAuth.Tests/Golden/<平台名>.json`）：
```json
{
  "source": "GITHUB",
  "constructorError": null,
  "authorizeUrl": "https://github.com/login/oauth/authorize?...",
  "scenarios": [
    {
      "name": "login_success",
      "requests": [ { "method": "POST", "url": "...", "params": null, "data": null, "headers": {} } ],
      "result": { "code": 2000, "data": { } },
      "error": null
    }
  ]
}
```

- [ ] **Step 1: pom.xml**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd">
  <modelVersion>4.0.0</modelVersion>
  <groupId>golden</groupId>
  <artifactId>justauth-golden</artifactId>
  <version>1.0</version>
  <properties>
    <maven.compiler.source>1.8</maven.compiler.source>
    <maven.compiler.target>1.8</maven.compiler.target>
    <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
  </properties>
  <dependencies>
    <dependency>
      <groupId>me.zhyd.oauth</groupId>
      <artifactId>JustAuth</artifactId>
      <version>1.16.7</version>
    </dependency>
    <dependency>
      <groupId>com.alibaba</groupId>
      <artifactId>fastjson</artifactId>
      <version>1.2.83</version>
    </dependency>
  </dependencies>
  <build>
    <plugins>
      <plugin>
        <groupId>org.codehaus.mojo</groupId>
        <artifactId>exec-maven-plugin</artifactId>
        <version>3.1.0</version>
        <configuration>
          <mainClass>golden.GoldenGenerator</mainClass>
        </configuration>
      </plugin>
    </plugins>
  </build>
</project>
```

- [ ] **Step 2: ReplayHttp.java**

```java
package golden;

import com.alibaba.fastjson.JSONObject;
import com.xkcoding.http.config.HttpConfig;
import com.xkcoding.http.support.AbstractHttp;
import com.xkcoding.http.support.HttpHeader;
import com.xkcoding.http.support.SimpleHttpResponse;

import java.util.*;

/**
 * 记录 JustAuth 发出的请求，并按顺序回放 fixture 中的响应
 */
public class ReplayHttp extends AbstractHttp {
    private final Deque<String> responses;
    private final List<JSONObject> requests = new ArrayList<>();

    public ReplayHttp(List<String> responses) {
        super(new HttpConfig());
        this.responses = new ArrayDeque<>(responses);
    }

    public List<JSONObject> getRequests() {
        return requests;
    }

    private SimpleHttpResponse record(String method, String url, Map<String, String> params, String data, HttpHeader header, Boolean encode) {
        JSONObject request = new JSONObject(true);
        request.put("method", method);
        request.put("url", url);
        request.put("params", params == null ? null : new LinkedHashMap<>(params));
        request.put("data", data);
        request.put("encode", encode);
        request.put("headers", header == null ? new LinkedHashMap<>() : new LinkedHashMap<>(header.getHeaders()));
        requests.add(request);
        if (responses.isEmpty()) {
            throw new IllegalStateException("No replay response for request #" + requests.size() + ": " + method + " " + url);
        }
        return new SimpleHttpResponse(true, 200, new HashMap<>(), responses.poll(), null);
    }

    @Override
    public SimpleHttpResponse get(String url) {
        return record("GET", url, null, null, null, null);
    }

    @Override
    public SimpleHttpResponse get(String url, Map<String, String> params, boolean encode) {
        return record("GET", url, params, null, null, encode);
    }

    @Override
    public SimpleHttpResponse get(String url, Map<String, String> params, HttpHeader header, boolean encode) {
        return record("GET", url, params, null, header, encode);
    }

    @Override
    public SimpleHttpResponse post(String url) {
        return record("POST", url, null, null, null, null);
    }

    @Override
    public SimpleHttpResponse post(String url, String data) {
        return record("POST", url, null, data, null, null);
    }

    @Override
    public SimpleHttpResponse post(String url, String data, HttpHeader header) {
        return record("POST", url, null, data, header, null);
    }

    @Override
    public SimpleHttpResponse post(String url, Map<String, String> params, boolean encode) {
        return record("POST", url, params, null, null, encode);
    }

    @Override
    public SimpleHttpResponse post(String url, Map<String, String> params, HttpHeader header, boolean encode) {
        return record("POST", url, params, null, header, encode);
    }
}
```

- [ ] **Step 3: GoldenGenerator.java**

```java
package golden;

import com.alibaba.fastjson.JSON;
import com.alibaba.fastjson.JSONArray;
import com.alibaba.fastjson.JSONObject;
import com.alibaba.fastjson.serializer.SerializerFeature;
import com.xkcoding.http.HttpUtil;
import me.zhyd.oauth.cache.AuthStateCache;
import me.zhyd.oauth.config.AuthConfig;
import me.zhyd.oauth.config.AuthDefaultSource;
import me.zhyd.oauth.exception.AuthException;
import me.zhyd.oauth.model.AuthCallback;
import me.zhyd.oauth.model.AuthResponse;
import me.zhyd.oauth.model.AuthToken;
import me.zhyd.oauth.request.AuthRequest;

import java.io.File;
import java.lang.reflect.InvocationTargetException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.*;

/**
 * 用法：mvn -q compile exec:java -Dexec.args="<fixturesDir> <goldenDir>"
 */
public class GoldenGenerator {

    /** 测试用内存 state 缓存，保证每个场景互不影响 */
    static class MemoryStateCache implements AuthStateCache {
        private final Map<String, String> map = new HashMap<>();
        public void cache(String key, String value) { map.put(key, value); }
        public void cache(String key, String value, long timeout) { map.put(key, value); }
        public String get(String key) { return map.get(key); }
        public boolean containsKey(String key) { return map.containsKey(key); }
    }

    public static void main(String[] args) throws Exception {
        File fixturesDir = new File(args[0]);
        File goldenDir = new File(args[1]);
        goldenDir.mkdirs();
        File[] files = fixturesDir.listFiles((d, n) -> n.endsWith(".json"));
        Arrays.sort(files);
        for (File file : files) {
            JSONObject fixture = JSON.parseObject(new String(Files.readAllBytes(file.toPath()), StandardCharsets.UTF_8));
            JSONObject golden = run(fixture);
            String text = JSON.toJSONString(golden, SerializerFeature.PrettyFormat, SerializerFeature.DisableCircularReferenceDetect);
            Files.write(new File(goldenDir, file.getName()).toPath(), text.getBytes(StandardCharsets.UTF_8));
            System.out.println("golden: " + file.getName());
        }
    }

    private static AuthRequest newRequest(AuthDefaultSource source, AuthConfig config, AuthStateCache cache) throws Exception {
        try {
            return source.getTargetClass().getConstructor(AuthConfig.class, AuthStateCache.class).newInstance(config, cache);
        } catch (InvocationTargetException e) {
            throw (Exception) e.getCause();
        }
    }

    private static JSONObject error(Exception e) {
        JSONObject err = new JSONObject(true);
        err.put("type", e.getClass().getName());
        if (e instanceof AuthException) {
            err.put("code", ((AuthException) e).getErrorCode());
            err.put("msg", ((AuthException) e).getErrorMsg());
        } else {
            err.put("msg", e.getMessage());
        }
        return err;
    }

    static JSONObject run(JSONObject fixture) throws Exception {
        AuthDefaultSource source = AuthDefaultSource.valueOf(fixture.getString("source"));
        AuthConfig config = fixture.getJSONObject("config").toJavaObject(AuthConfig.class);
        String state = fixture.getString("state");

        JSONObject golden = new JSONObject(true);
        golden.put("source", source.name());

        // 授权地址
        HttpUtil.setHttp(new ReplayHttp(Collections.emptyList()));
        try {
            AuthRequest request = newRequest(source, config, new MemoryStateCache());
            golden.put("constructorError", null);
            golden.put("authorizeUrl", request.authorize(state));
        } catch (Exception e) {
            golden.put("constructorError", error(e));
            golden.put("authorizeUrl", null);
            golden.put("scenarios", new JSONArray());
            return golden;
        }

        JSONArray scenarios = new JSONArray();
        for (Object item : fixture.getJSONArray("scenarios")) {
            JSONObject scenario = (JSONObject) item;
            List<String> responses = scenario.getJSONArray("responses").toJavaList(String.class);
            ReplayHttp http = new ReplayHttp(responses);
            HttpUtil.setHttp(http);

            JSONObject out = new JSONObject(true);
            out.put("name", scenario.getString("name"));
            try {
                MemoryStateCache cache = new MemoryStateCache();
                AuthRequest request = newRequest(source, config, cache);
                request.authorize(state);
                AuthResponse<?> result;
                switch (scenario.getString("action")) {
                    case "login":
                        result = request.login(scenario.getJSONObject("callback").toJavaObject(AuthCallback.class));
                        break;
                    case "refresh":
                        result = request.refresh(scenario.getJSONObject("token").toJavaObject(AuthToken.class));
                        break;
                    case "revoke":
                        result = request.revoke(scenario.getJSONObject("token").toJavaObject(AuthToken.class));
                        break;
                    default:
                        throw new IllegalArgumentException("unknown action " + scenario.getString("action"));
                }
                out.put("result", JSON.toJSON(result));
                out.put("error", null);
            } catch (Exception e) {
                out.put("result", null);
                out.put("error", error(e));
            }
            out.put("requests", http.getRequests());
            scenarios.add(out);
        }
        golden.put("scenarios", scenarios);
        return golden;
    }
}
```

`tools/justauth-golden/README.md`：
```markdown
# JustAuth 标准答案生成器

读取 `Come.CollectiveOAuth.Tests/Fixtures/*.json`，用 JustAuth 1.16.7 执行授权/登录/刷新/撤销，
把授权地址、发出的请求、返回结果写入 `Come.CollectiveOAuth.Tests/Golden/`。

    cd tools/justauth-golden
    mvn -q compile exec:java -Dexec.args="../../Come.CollectiveOAuth.Tests/Fixtures ../../Come.CollectiveOAuth.Tests/Golden"

需要 JDK 8+ 与 Maven。新增或修改 fixture 后重新运行，并提交生成的 golden 文件。
```

- [ ] **Step 4: 运行验证（先用临时 fixture）**

创建 `Come.CollectiveOAuth.Tests/Fixtures/GITHUB.json`（内容见 Task 17 Step 1），然后：
```bash
cd tools/justauth-golden
mvn -q compile exec:java -Dexec.args="../../Come.CollectiveOAuth.Tests/Fixtures ../../Come.CollectiveOAuth.Tests/Golden"
cat ../../Come.CollectiveOAuth.Tests/Golden/GITHUB.json
```
Expected: 打印 `golden: GITHUB.json`；golden 中 `authorizeUrl` 以 `https://github.com/login/oauth/authorize?response_type=code&client_id=cid` 开头，`login_success` 场景 `result.code` 为 2000，`requests` 有 2 条（POST access_token、GET user，后者 headers 含 `Authorization: token gho_abc`）。

- [ ] **Step 5: 提交**

```bash
git add tools/justauth-golden Come.CollectiveOAuth.Tests/Fixtures Come.CollectiveOAuth.Tests/Golden
git commit -m "test: 新增 JustAuth 标准答案生成器（录制请求/回放响应）"
```

（`tools/justauth-golden/target/` 需加入 `.gitignore`：在仓库根 `.gitignore` 末尾追加一行 `tools/justauth-golden/target/`。）

---

### Task 16（阶段 1b）: C# 差分比对框架

**Files:**
- Create: `Come.CollectiveOAuth.Tests/Golden/GoldenRequestFactory.cs`（放在 `Support/` 下）
- Create: `Come.CollectiveOAuth.Tests/Support/GoldenNormalizer.cs`
- Create: `Come.CollectiveOAuth.Tests/GoldenTests.cs`

比对规则（`GoldenNormalizer`）：
1. **URL**：拆成「base（`?` 之前）+ 参数多重集合」；参数名、值各做一次 `urlDecode` 后比较，忽略顺序。
2. **请求**：Java 的 `params`（GET）并入 URL 参数；Java POST 的 `params` 视为表单体，`data` 视为原始体；C# 表单体（`contentType` 为 form）按 `&`/`=` 拆成字典并解码；原始体若两边都能解析为 JSON 则按 JSON 树比较，否则按字符串比较。headers 只比较双方都显式传入的键集合及其值（忽略 `Content-Type`、`Host`、`User-Agent`）。
3. **结果**：C# 结果序列化时忽略 null、枚举用名称；删除 C# 独有字段 `originalUser`、`originalUserStr`；`data.source` 与 `msg` 中的 `[平台名]` 用 `AuthSourceNames.toJustAuthName` 换算后比较；数字一律按十进制数值比较。
4. **异常**：Java `error`（login 以外的场景可能抛出）与 C# 抛出的 `AuthException` 比较 `code`、`msg`。
5. **忽略项**：fixture 可声明 `"ignore": { "params": ["oauth_nonce", "oauth_timestamp", "oauth_signature", "timestamp", "sign"] }`，比较 URL/表单/header 内这些参数时只比较“存在性”。

- [ ] **Step 1: GoldenNormalizer.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Tests.Support
{
    internal static class GoldenNormalizer
    {
        public static JObject Url(string url, IDictionary<string, string> extraParams, ISet<string> ignore)
        {
            if (url == null)
            {
                return null;
            }
            int index = url.IndexOf('?');
            string baseUrl = index < 0 ? url : url.Substring(0, index);
            var pairs = new List<KeyValuePair<string, string>>();
            if (index >= 0)
            {
                pairs.AddRange(ParsePairs(url.Substring(index + 1)));
            }
            if (extraParams != null)
            {
                pairs.AddRange(extraParams.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value)));
            }
            return new JObject { ["base"] = baseUrl, ["params"] = Params(pairs, ignore) };
        }

        public static IEnumerable<KeyValuePair<string, string>> ParsePairs(string query)
        {
            foreach (var part in query.Split('&'))
            {
                if (part.Length == 0)
                {
                    continue;
                }
                int eq = part.IndexOf('=');
                string key = eq < 0 ? part : part.Substring(0, eq);
                string value = eq < 0 ? null : part.Substring(eq + 1);
                yield return new KeyValuePair<string, string>(GlobalAuthUtil.urlDecode(key), value == null ? null : GlobalAuthUtil.urlDecode(value));
            }
        }

        public static JArray Params(IEnumerable<KeyValuePair<string, string>> pairs, ISet<string> ignore)
        {
            return new JArray(pairs
                .Select(kv => ignore.Contains(kv.Key) ? kv.Key + "=<ignored>" : kv.Key + "=" + (kv.Value ?? ""))
                .OrderBy(s => s, StringComparer.Ordinal));
        }

        public static JToken Body(string body, bool isForm, ISet<string> ignore)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }
            if (isForm)
            {
                return Params(ParsePairs(body), ignore);
            }
            try
            {
                return Numbers(JToken.Parse(body));
            }
            catch (JsonReaderException)
            {
                return body;
            }
        }

        public static JObject Headers(IDictionary<string, string> headers, ISet<string> ignore)
        {
            var result = new JObject();
            foreach (var kv in headers.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = kv.Key.ToUpperInvariant();
                if (key == "CONTENT-TYPE" || key == "HOST" || key == "USER-AGENT")
                {
                    continue;
                }
                result[key] = ignore.Contains(kv.Key) ? "<ignored>" : kv.Value;
            }
            return result;
        }

        /// <summary>
        /// 把数字统一为 decimal（避免 1 与 1.0 不等），并删除值为 null 的属性（fastjson 输出时会省略 null）
        /// </summary>
        public static JToken Numbers(JToken token)
        {
            switch (token)
            {
                case JObject obj:
                    foreach (var prop in obj.Properties().ToList())
                    {
                        if (prop.Value.Type == JTokenType.Null)
                        {
                            prop.Remove();
                            continue;
                        }
                        prop.Value = Numbers(prop.Value);
                    }
                    return obj;
                case JArray arr:
                    for (int i = 0; i < arr.Count; i++)
                    {
                        arr[i] = Numbers(arr[i]);
                    }
                    return arr;
                case JValue v when v.Type == JTokenType.Integer || v.Type == JTokenType.Float:
                    return new JValue(Convert.ToDecimal(v.Value));
                default:
                    return token;
            }
        }

        public static JToken Result(object csharpResult)
        {
            if (csharpResult == null)
            {
                return null;
            }
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = { new StringEnumConverter() } };
            var token = JToken.Parse(JsonConvert.SerializeObject(csharpResult, settings));
            RemoveCsharpOnly(token);
            if (token is JObject obj)
            {
                if (obj["msg"] is JValue msg && msg.Type == JTokenType.String)
                {
                    obj["msg"] = MapSourceInMessage((string)msg);
                }
                if (obj["data"] is JObject data && data["source"] is JValue source)
                {
                    data["source"] = AuthSourceNames.toJustAuthName((string)source);
                }
            }
            return Numbers(token);
        }

        public static string MapSourceInMessage(string msg)
        {
            int start = msg.LastIndexOf('[');
            if (start < 0 || !msg.EndsWith("]"))
            {
                return msg;
            }
            string name = msg.Substring(start + 1, msg.Length - start - 2);
            return msg.Substring(0, start + 1) + AuthSourceNames.toJustAuthName(name) + "]";
        }

        private static void RemoveCsharpOnly(JToken token)
        {
            if (token is JObject obj)
            {
                obj.Remove("originalUser");
                obj.Remove("originalUserStr");
                foreach (var prop in obj.Properties())
                {
                    RemoveCsharpOnly(prop.Value);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var child in arr)
                {
                    RemoveCsharpOnly(child);
                }
            }
        }
    }
}
```

- [ ] **Step 2: GoldenRequestFactory.cs**

```csharp
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Request;

namespace Come.CollectiveOAuth.Tests.Support
{
    /// <summary>
    /// JustAuth 平台名 → C# 请求类。阶段 2/3 每对齐或新增一个平台就在这里登记
    /// </summary>
    internal static class GoldenRequestFactory
    {
        private static readonly Dictionary<string, Func<ClientConfig, IAuthStateCache, IAuthRequest>> creators =
            new Dictionary<string, Func<ClientConfig, IAuthStateCache, IAuthRequest>>
            {
                { "GITHUB", (c, s) => new GithubAuthRequest(c, s) },
            };

        public static bool IsRegistered(string justAuthName)
        {
            return creators.ContainsKey(justAuthName);
        }

        public static IAuthRequest Create(string justAuthName, ClientConfig config, IAuthStateCache cache)
        {
            return creators[justAuthName](config, cache);
        }
    }
}
```

- [ ] **Step 3: GoldenTests.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Tests.Support;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    [Collection("HttpSender")]
    public class GoldenTests
    {
        private static string Dir(string name) => Path.Combine(AppContext.BaseDirectory, name);

        public static IEnumerable<object[]> Cases()
        {
            foreach (var file in Directory.GetFiles(Dir("Fixtures"), "*.json").OrderBy(f => f))
            {
                var fixture = JObject.Parse(File.ReadAllText(file));
                yield return new object[] { fixture.Value<string>("source"), "authorize" };
                foreach (var scenario in fixture["scenarios"])
                {
                    yield return new object[] { fixture.Value<string>("source"), scenario.Value<string>("name") };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void Matches_justauth(string source, string scenarioName)
        {
            Assert.True(GoldenRequestFactory.IsRegistered(source), $"{source} 未在 GoldenRequestFactory 登记");
            var fixture = JObject.Parse(File.ReadAllText(Path.Combine(Dir("Fixtures"), source + ".json")));
            var golden = JObject.Parse(File.ReadAllText(Path.Combine(Dir("Golden"), source + ".json")));
            var ignore = new HashSet<string>(fixture["ignore"]?["params"]?.ToObject<string[]>() ?? new string[0]);
            var config = fixture["config"].ToObject<ClientConfig>();
            string state = fixture.Value<string>("state");

            if (scenarioName == "authorize")
            {
                var goldenCtorError = golden["constructorError"];
                if (goldenCtorError != null && goldenCtorError.Type != JTokenType.Null)
                {
                    var ex = Assert.Throws<AuthException>(() => GoldenRequestFactory.Create(source, config, new DefaultAuthStateCache()));
                    Assert.Equal(goldenCtorError.Value<int>("code"), ex.errorCode);
                    Assert.Equal(goldenCtorError.Value<string>("msg"), GoldenNormalizer.MapSourceInMessage(ex.errorMsg));
                    return;
                }
                var request = GoldenRequestFactory.Create(source, config, new DefaultAuthStateCache());
                AssertJson(GoldenNormalizer.Url(golden.Value<string>("authorizeUrl"), null, ignore), GoldenNormalizer.Url(request.authorize(state), null, ignore), "authorizeUrl");
                return;
            }

            var fixtureScenario = fixture["scenarios"].First(s => s.Value<string>("name") == scenarioName);
            var goldenScenario = golden["scenarios"].First(s => s.Value<string>("name") == scenarioName);
            var sender = new RecordingHttpSender(fixtureScenario["responses"].ToObject<string[]>());
            var original = HttpUtils.Sender;
            HttpUtils.Sender = sender;
            object result = null;
            Exception error = null;
            try
            {
                var request = GoldenRequestFactory.Create(source, config, new DefaultAuthStateCache());
                request.authorize(state);
                switch (fixtureScenario.Value<string>("action"))
                {
                    case "login":
                        result = request.login(fixtureScenario["callback"].ToObject<AuthCallback>());
                        break;
                    case "refresh":
                        result = request.refresh(fixtureScenario["token"].ToObject<AuthToken>());
                        break;
                    case "revoke":
                        result = request.revoke(fixtureScenario["token"].ToObject<AuthToken>());
                        break;
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                HttpUtils.Sender = original;
            }

            // 请求序列
            var goldenRequests = (JArray)goldenScenario["requests"];
            Assert.True(goldenRequests.Count == sender.requests.Count,
                $"请求数量不同：Java {goldenRequests.Count}，C# {sender.requests.Count}\nJava: {goldenRequests}\nC#: {string.Join("\n", sender.requests.Select(r => r.method + " " + r.url))}");
            for (int i = 0; i < goldenRequests.Count; i++)
            {
                var g = (JObject)goldenRequests[i];
                var c = sender.requests[i];
                Assert.Equal(g.Value<string>("method"), c.method);
                var gParams = g["params"]?.Type == JTokenType.Object ? g["params"].ToObject<Dictionary<string, string>>() : null;
                bool gIsGet = g.Value<string>("method") == "GET";
                AssertJson(GoldenNormalizer.Url(g.Value<string>("url"), gIsGet ? gParams : null, ignore), GoldenNormalizer.Url(c.url, null, ignore), $"request[{i}].url");

                JToken gBody = !gIsGet && gParams != null
                    ? GoldenNormalizer.Params(gParams.Select(kv => kv), ignore)
                    : GoldenNormalizer.Body(g.Value<string>("data"), false, ignore);
                JToken cBody = GoldenNormalizer.Body(c.body, c.contentType == "application/x-www-form-urlencoded", ignore);
                AssertJson(gBody, cBody, $"request[{i}].body");

                AssertJson(GoldenNormalizer.Headers(g["headers"].ToObject<Dictionary<string, string>>(), ignore), GoldenNormalizer.Headers(c.headers, ignore), $"request[{i}].headers");
            }

            // 结果或异常
            var goldenError = goldenScenario["error"];
            if (goldenError != null && goldenError.Type != JTokenType.Null)
            {
                var authException = Assert.IsType<AuthException>(error);
                Assert.Equal(goldenError.Value<int?>("code") ?? 0, authException.errorCode);
                Assert.Equal(goldenError.Value<string>("msg"), authException.errorMsg == null ? null : GoldenNormalizer.MapSourceInMessage(authException.errorMsg));
                return;
            }
            Assert.Null(error);
            AssertJson(GoldenNormalizer.Numbers(goldenScenario["result"].DeepClone()), GoldenNormalizer.Result(result), "result");
        }

        private static void AssertJson(JToken expected, JToken actual, string what)
        {
            Assert.True(JToken.DeepEquals(expected, actual), $"{what} 不一致\nJava: {expected}\nC#:   {actual}");
        }
    }
}
```

- [ ] **Step 4: 运行（预期 GitHub 失败，作为 Task 17 的红灯）**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter GoldenTests`
Expected: GitHub 的 `authorize`、`login_success` 等用例**失败**，失败信息逐项列出 Java 与 C# 的差异（如 C# scope 为 `user`、未带 `Authorization` 头等）。若报的是框架本身的错误（如找不到 Fixtures 目录），先修框架。

- [ ] **Step 5: 提交**

```bash
git add Come.CollectiveOAuth.Tests
git commit -m "test: 新增 Java↔C# 差分比对框架"
```

---

### Task 17（阶段 1b）: GitHub 全链路对齐（验证样本）

**Files:**
- Create/Modify: `Come.CollectiveOAuth.Tests/Fixtures/GITHUB.json`、`Golden/GITHUB.json`
- Modify: `Come.CollectiveOAuth/Request/AuthRequests/GithubAuthRequest.cs`

- [ ] **Step 1: fixture**

`Come.CollectiveOAuth.Tests/Fixtures/GITHUB.json`：
```json
{
  "source": "GITHUB",
  "config": { "clientId": "cid", "clientSecret": "csecret", "redirectUri": "https://www.justauth.cn/callback/github" },
  "state": "fixedstate000000000000000000000",
  "scenarios": [
    {
      "name": "login_success",
      "action": "login",
      "callback": { "code": "code123", "state": "fixedstate000000000000000000000" },
      "responses": [
        "access_token=gho_abc&scope=user&token_type=bearer",
        "{\"login\":\"octocat\",\"id\":583231,\"avatar_url\":\"https://avatars.githubusercontent.com/u/583231?v=4\",\"name\":\"The Octocat\",\"company\":\"@github\",\"blog\":\"https://github.blog\",\"location\":\"San Francisco\",\"email\":null,\"bio\":null,\"public_repos\":8,\"site_admin\":false}"
      ]
    },
    {
      "name": "login_token_error",
      "action": "login",
      "callback": { "code": "bad", "state": "fixedstate000000000000000000000" },
      "responses": [
        "error=bad_verification_code&error_description=The+code+passed+is+incorrect+or+expired.&error_uri=https%3A%2F%2Fdocs.github.com"
      ]
    },
    {
      "name": "login_illegal_state",
      "action": "login",
      "callback": { "code": "code123", "state": "otherstate" },
      "responses": []
    },
    {
      "name": "login_missing_code",
      "action": "login",
      "callback": { "state": "fixedstate000000000000000000000" },
      "responses": []
    }
  ]
}
```

- [ ] **Step 2: 生成 golden**

```bash
cd tools/justauth-golden
mvn -q compile exec:java -Dexec.args="../../Come.CollectiveOAuth.Tests/Fixtures ../../Come.CollectiveOAuth.Tests/Golden"
```
Expected: `golden: GITHUB.json`；`login_token_error` 的 `result` 为 `{"code":5000,"msg":"The code passed is incorrect or expired."}`；`login_illegal_state` 为 `{"code":5009,"msg":"Illegal state [GITHUB]"}`。

- [ ] **Step 3: 运行确认失败**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter GoldenTests`
Expected: GitHub 用例失败。

- [ ] **Step 4: 按 Java 版重写 GithubAuthRequest**

`GithubAuthRequest.cs` 整体替换为：
```csharp
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Github登录，对齐 JustAuth AuthGithubRequest
     */
    public class GithubAuthRequest : DefaultAuthRequest
    {
        public GithubAuthRequest(ClientConfig config) : base(config, new GithubAuthSource())
        {
        }

        public GithubAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GithubAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var res = GlobalAuthUtil.parseStringToMap(response);
            this.checkResponse(res.ContainsKey("error"), res.TryGetValue("error_description", out var description) ? description : null);

            var authToken = new AuthToken();
            authToken.accessToken = res.TryGetValue("access_token", out var accessToken) ? accessToken : null;
            authToken.scope = res.TryGetValue("scope", out var scope) ? scope : null;
            authToken.tokenType = res.TryGetValue("token_type", out var tokenType) ? tokenType : null;
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var header = new Dictionary<string, object> { { "Authorization", "token " + authToken.accessToken } };
            string response = HttpUtils.RequestGet(UrlBuilder.fromBaseUrl(source.userInfo()).build(), header, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj.containsKey("error"), obj.getString("error_description"));

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("login");
            authUser.avatar = obj.getString("avatar_url");
            authUser.blog = obj.getString("blog");
            authUser.nickname = obj.getString("name");
            authUser.company = obj.getString("company");
            authUser.location = obj.getString("location");
            authUser.email = obj.getString("email");
            authUser.remark = obj.getString("bio");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        private void checkResponse(bool error, string errorDescription)
        {
            if (error)
            {
                throw new AuthException(errorDescription);
            }
        }

        public override string authorize(string state)
        {
            // Java 版传 encode=true 后交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthGithubScope.values())))
                .build();
        }
    }
}
```

- [ ] **Step 5: 运行直到通过**

Run: `dotnet test Come.CollectiveOAuth.Tests --filter GoldenTests`
Expected: GitHub 5 个用例（authorize + 4 个场景）全部通过。若失败，按失败信息中的 Java/C# 差异修正 **C# 业务代码**；只有确认是比对规则问题（如表示差异而非行为差异）时才修改 `GoldenNormalizer`，并在提交信息中写明原因。

- [ ] **Step 6: 全量验证**

Run:
```bash
dotnet test Come.CollectiveOAuth.Tests
dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj
dotnet build Come.AspNetCore.Sample/Come.AspNetCore.Sample.csproj
```
Expected: 测试全部通过；两个构建均 `0 个错误`。

- [ ] **Step 7: 提交**

```bash
git add Come.CollectiveOAuth/Request/AuthRequests/GithubAuthRequest.cs Come.CollectiveOAuth.Tests
git commit -m "feat: GitHub 对齐 JustAuth 并通过差分比对"
```

---

## 阶段完成后

向用户汇报：完成的任务、测试数量与结果、有意差异清单的增补（默认超时改为 3000ms、默认 state 缓存改为 3 分钟绝对过期、`refresh`/`revoke` 未实现时改为抛 `AuthException`、`AuthCallback.code` 回退到 `auth_code`、`oauthCallbackConfirmed` 改为 `bool?`），以及需要用户手动处理的 `Web.config` 中 `targetFramework` 修改。然后为阶段 2 编写下一份计划。
