# 阶段 2：已有平台对齐 JustAuth 1.16.7 实施计划

> **For agentic workers:** 本计划按批次并行执行，每批由一个子代理在独立 worktree 中完成，主代理负责合并与全量验证。

**Goal:** 把 C# 现有的 30 个平台（GitHub 已在阶段 1b 完成）逐个对齐 JustAuth 1.16.7，并全部通过 Java↔C# 差分测试。

**前置条件：** 阶段 1/1b 已完成（`ClientConfig` 全字段、`AuthException`、`JsonExtensions`、`GlobalAuthUtil` Java 等价方法、Scope 类、`DefaultAuthRequest.getScopes`、差分框架）。GitHub（`GithubAuthRequest.cs` + `Fixtures/GITHUB.json`）是**参考样板**，移植前先读它。

**参考源码：** `/tmp/JustAuth`（Windows 路径用 `cygpath -w /tmp/JustAuth` 获取）。

---

## 每个平台的移植流程（逐个平台执行，每个平台一次提交）

1. **读 Java**：`src/main/java/me/zhyd/oauth/request/Auth<X>Request.java`（含父类）与 `config/AuthDefaultSource.java` 中该平台的段落。
2. **对齐 AuthSource**：`Come.CollectiveOAuth/Config/AuthSources/<X>AuthSource.cs` 的 `authorize/accessToken/userInfo/revoke/refresh` 返回值与 Java 完全一致；Java 未覆盖的 `revoke`/`refresh` 改为 `throw new AuthException(AuthResponseStatus.UNSUPPORTED);`（Java `AuthSource` 默认行为）。`getName()` 不改。
3. **写 fixture**：`Come.CollectiveOAuth.Tests/Fixtures/<JustAuth平台名>.json`，至少包含：
   - `login_success`：响应体参照平台官方文档的真实字段（覆盖 Java 读取的每个字段）；
   - 至少一个平台错误场景（Java `checkResponse` 会抛出的那种）；
   - Java 实现了 `refresh`/`revoke` 的，各加一个成功场景；
   - `redirectUri` 不得含 `?`、`&`；`state` 用 32 位以内的固定串；
   - 含随机/时间字段（nonce、timestamp、sign）的，在 `"ignore": {"params": [...]}` 中声明。
4. **生成 golden**：
   ```bash
   cd tools/justauth-golden
   mvn -q compile exec:java -Dexec.args="../../Come.CollectiveOAuth.Tests/Fixtures ../../Come.CollectiveOAuth.Tests/Golden"
   ```
   打开生成的 `Golden/<平台>.json`，确认场景结果符合预期（不应出现 `No replay response` 之类的工具错误）。
5. **登记**：在 `Come.CollectiveOAuth.Tests/Support/GoldenRequestFactory.cs` 中**本批次的标记注释下方**加一行，键为 JustAuth 平台名。
6. **红灯**：`dotnet test Come.CollectiveOAuth.Tests --filter GoldenTests` 看到该平台失败。
7. **重写 C# 请求类**：逐行对照 Java 改写 `Request/AuthRequests/<X>AuthRequest.cs`，保留现有 C# 类名与两个构造函数 `(ClientConfig)`、`(ClientConfig, IAuthStateCache)`。
8. **绿灯**：该平台全部用例通过，且 `dotnet test Come.CollectiveOAuth.Tests` 全量通过、`dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj` 三个框架 0 错误。
9. **提交**：`git commit -m "feat: <平台> 对齐 JustAuth 并通过差分比对"`，结尾加 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。

## Java → C# 对照规则

| Java | C# |
|---|---|
| `JSONObject.parseObject(s)` | `JsonExtensions.parseObject(s)`（返回 `JObject`） |
| `obj.getString/getIntValue/getInteger/getLongValue/getBooleanValue/getJSONObject/getJSONArray/containsKey` | 同名扩展方法（`using Come.CollectiveOAuth.Utils;`） |
| `GlobalAuthUtils.xxx` | `GlobalAuthUtil.xxx`（同名方法已移植：`urlEncode/urlDecode/parseStringToMap/parseMapToString/generate*Signature/md5/getTimestamp/generateNonce`） |
| `AuthUserGender.getRealGender/getWechatRealGender` | `GlobalAuthUtil.getRealGender/getWechatRealGender` |
| `new AuthException(msg)` / `(status, source)` / `(code, msg, source)` | 同签名 `Come.CollectiveOAuth.Exceptions.AuthException` |
| `new HttpUtils(config.getHttpConfig()).get(url).getBody()` | `HttpUtils.RequestGet(url, null, config.httpConfig)` |
| `.get(url, params, header, encode)` | 用 `UrlBuilder` 把 params 拼进 URL 后 `HttpUtils.RequestGet(url, headerDict, config.httpConfig)` |
| `.post(url)` | `HttpUtils.RequestPost(url, null, null, null, config.httpConfig)` |
| `.post(url, data)` / `.post(url, data, header)` | `HttpUtils.RequestPost(url, data, headerDict, null, config.httpConfig)` |
| `.post(url, map, encode)` / `.post(url, map, header, encode)` | `HttpUtils.RequestFormPost(url, GlobalAuthUtil.parseMapToString(map, true), headerDict, null, config.httpConfig)` |
| `HttpHeader().add(k, v)` | `new Dictionary<string, object> { { k, v } }` |
| `AuthUser.builder().rawUserInfo(obj)` | `authUser.rawUserInfo = obj;` 另保留兼容字段 `originalUser = obj; originalUserStr = response;` |
| `source.toString()` / `source.getName()` | `source.getName()` |
| `AuthScopeUtils.getDefaultScopes(AuthXxxScope.values())` | 同名，Scope 类在 `Come.CollectiveOAuth.Enums.Scope` |
| `StringUtils.isEmpty/isNotEmpty` | `string.IsNullOrEmpty` / `!string.IsNullOrEmpty` |
| `config.isUnionId()` | `config.isUnionId()` |
| 枚举比较 `source == AuthDefaultSource.X` | `AuthSourceNames.toJustAuthName(source.getName()) == "X"` |

**URL 编码（有意差异，最容易出错）：** C# `UrlBuilder.build()` 默认编码参数值。凡是 Java 先手动 `urlEncode(...)` 或 `getScopes(sep, true, ...)` 再交给 `UrlBuilder.build()` 的值，C# 一律传**原值**（`getScopes(sep, false, ...)`、不调用 `urlEncode`）。Java 显式 `build(true)` 的，C# 用 `build()`。

**`authorize` 里的 `#wechat_redirect` 这类片段**：拼在 `build()` 结果之后，不能作为参数值。

## 禁止事项

- 不得修改共享基础设施：`DefaultAuthRequest`、`Utils/*`、`Models/*`、`Config/ClientConfig.cs`、`Tests/Support/GoldenNormalizer.cs`、`GoldenTests.cs`、`tools/justauth-golden/*`。确有缺口时，**停止并在报告中说明**需要什么，不要绕过。
- 平台私有的辅助方法放在该平台的请求类里。
- 不得为了让测试通过而修改 golden 文件或放宽比对规则；golden 只能由 Java 工具生成。
- 不要删除平台类里与 Java 无关但属于公开 API 的方法（如额外的公开构造函数）。

## 批次

| 批次 | 平台（JustAuth 名 → C# 类） | 需要的 Java 父类 |
|---|---|---|
| A 微信 | WECHAT_MP→`WeChatMpAuthRequest`、WECHAT_OPEN→`WeChatOpenAuthRequest`、WECHAT_ENTERPRISE_WEB→`WeChatEnterpriseAuthRequest`、WECHAT_ENTERPRISE→`WeChatEnterpriseScanAuthRequest` | `AbstractAuthWeChatEnterpriseRequest`（C# 新建 `AbstractWeChatEnterpriseAuthRequest.cs`） |
| B 国内一 | GITEE、BAIDU、MI→`XiaoMiAuthRequest`、OSCHINA、CODING、WEIBO、QQ→`QQAuthRequest` | — |
| C 国内二 | DINGTALK→`DingTalkScanAuthRequest`、DOUYIN→`DouYinAuthRequest`、TOUTIAO、HUAWEI、KUJIALE、MEITUAN、ELEME、TAOBAO | `AbstractAuthDingtalkRequest`（C# 新建 `AbstractDingTalkAuthRequest.cs`） |
| D 海外 | LINKEDIN、GOOGLE、FACEBOOK→`FackbookAuthRequest`、MICROSOFT、PINTEREST、STACK_OVERFLOW、GITLAB、TEAMBITION、RENREN | `AbstractAuthMicrosoftRequest`（C# 新建 `AbstractMicrosoftAuthRequest.cs`） |
| E 特殊 | TWITTER→`TwitteAuthRequest`（当前整类被注释，需完整实现；OAuth1 签名的 nonce/timestamp/signature 列入 ignore）、ALIPAY→`AlipayMpAuthRequest`（Java 走支付宝 SDK，HTTP 无法拦截：只比对 `authorize`，登录逻辑人工逐行对照，fixture 不写 login 场景） | — |

## 完成标准

- 30 个平台全部在 `GoldenRequestFactory` 登记，`dotnet test` 全量通过。
- 三个目标框架 0 错误；Core 示例 0 错误；MVC 示例代码可在 net462 下编译。
- 每批报告：完成的平台、每个平台的场景数、发现的原有 bug、需要主代理处理的基础设施缺口。
