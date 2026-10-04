# CollectiveOAuth 移植 JustAuth —— 交接文档

> 写给接手的 AI 工具或开发者。读完这一份即可继续工作，不需要之前的对话记录。
> 最后更新：2026-10-04，分支 `feature/justauth-port`，HEAD `edca403`，领先 `master`（`7eda129`）73 个提交，未 push。

---

## 1. 项目目标

CollectiveOAuth 是 Java 库 [JustAuth](https://github.com/justauth/JustAuth) 的 C# 移植版，统一封装了几十个第三方 OAuth 登录。
当前任务：**以 JustAuth 1.16.7（提交 `694bbf1`）为基准，把它的全部功能移植过来，功能行为与 Java 版一致。**

用户已确定的四项决策（不要再改）：

| 决策 | 结论 |
|---|---|
| 一致的标准 | 功能行为与 1.16.7 一致；保留安全/健壮性修复，差异逐条记录（见第 5 节） |
| 对外 API | 保留现有 C# API（`ClientConfig`、`IAuthRequest`、`XxxAuthRequest`、小写方法名、同步接口），只扩展 |
| 目标框架 | 类库 `net462;netstandard2.0;net8.0`；Core 示例 `net8.0`；MVC 示例 `.NET Framework 4.6.2` |
| 验证方式 | Java↔C# 差分对照测试（第 4 节） |

设计文档：`docs/superpowers/specs/2026-10-04-justauth-port-design.md`
阶段计划：`docs/superpowers/plans/2026-10-04-phase1-foundation.md`、`docs/superpowers/plans/2026-10-04-phase2-align-platforms.md`

---

## 2. 进度

| 阶段 | 内容 | 状态 |
|---|---|---|
| 0 | 提交前期修复（state 防重放、URL 编码、HTTP 错误透传等） | ✅ |
| 1 地基 | 框架升级、`ClientConfig` 补齐 24 个字段、`AuthException`、默认缓存、30 个 Scope 类、PKCE、工具类、`AuthChecker`、`DefaultAuthRequest` 对齐 | ✅ |
| 1b 差分框架 | Java 标准答案生成器 + C# 比对框架 | ✅ |
| 2 已有平台对齐 | C# 原有 31 个平台全部对齐并通过差分 | ✅ |
| **3 新增平台** | JustAuth 有、C# 没有的 21 个平台 | ⏳ **下一步** |
| 4 收尾 | 示例工厂覆盖全部平台；README 补差异清单与平台对照表；可选移除 TopSdk 引用 | ⏳ |

**当前测试**：`dotnet test Come.CollectiveOAuth.Tests` → 234 个全部通过，其中差分场景 146 个，授权地址比对 31 个。三个框架构建均 0 错误。

### 2.1 已完成平台（31 个）

JustAuth 名 → C# 请求类（`Come.CollectiveOAuth/Request/AuthRequests/`）：

| 国内 | 海外 |
|---|---|
| WECHAT_MP → `WeChatMpAuthRequest` | GITHUB → `GithubAuthRequest` |
| WECHAT_OPEN → `WeChatOpenAuthRequest` | GOOGLE → `GoogleAuthRequest` |
| WECHAT_ENTERPRISE（扫码）→ `WeChatEnterpriseScanAuthRequest` | FACEBOOK → `FackbookAuthRequest`（拼写保留） |
| WECHAT_ENTERPRISE_WEB → `WeChatEnterpriseAuthRequest` | MICROSOFT → `MicrosoftAuthRequest` |
| ALIPAY → `AlipayMpAuthRequest` | LINKEDIN → `LinkedinAuthRequest` |
| DINGTALK → `DingTalkScanAuthRequest` | PINTEREST → `PinterestAuthRequest` |
| GITEE、BAIDU、OSCHINA、CODING、WEIBO | STACK_OVERFLOW → `StackOverflowAuthRequest` |
| MI → `XiaoMiAuthRequest` | GITLAB → `GitlabAuthRequest` |
| QQ → `QQAuthRequest` | TEAMBITION → `TeambitionAuthRequest` |
| DOUYIN → `DouYinAuthRequest` | RENREN → `RenrenAuthRequest` |
| TOUTIAO、HUAWEI、KUJIALE、MEITUAN、ELEME、TAOBAO | TWITTER → `TwitteAuthRequest`（拼写保留） |

已有的抽象父类：`AbstractWeChatEnterpriseAuthRequest`、`AbstractDingTalkAuthRequest`、`AbstractMicrosoftAuthRequest`。

### 2.2 阶段 3 待新增（21 个）

| JustAuth 平台名 | Java 请求类 | 备注 |
|---|---|---|
| DINGTALK_V2 | `AuthDingTalkV2Request` | |
| DINGTALK_ACCOUNT | `AuthDingTalkAccountRequest` | 继承 `AbstractDingTalkAuthRequest` |
| CSDN | `AuthCsdnRequest` | |
| MICROSOFT_CN | `AuthMicrosoftCnRequest` | 继承 `AbstractMicrosoftAuthRequest`，覆盖 `checkConfig` |
| HUAWEI_V3 | `AuthHuaweiV3Request` | |
| WECHAT_ENTERPRISE_V2 | `AuthWeChatEnterpriseQrcodeV2Request` | |
| WECHAT_ENTERPRISE_QRCODE_THIRD | `AuthWeChatEnterpriseThirdQrcodeRequest` | 自己调用 `checkState`，逻辑特殊 |
| FEISHU | `AuthFeishuRequest` | |
| JD | `AuthJdRequest` | 签名已有 `GlobalAuthUtil.generateJdSignature` |
| ALIYUN | `AuthAliyunRequest` | |
| XMLY（喜马拉雅） | `AuthXmlyRequest` | 签名已有 `generateXmlySignature`；需 `deviceId/clientOsType/packId` |
| AMAZON | `AuthAmazonRequest` | 支持 PKCE（`PkceUtil` 已有） |
| SLACK | `AuthSlackRequest` | |
| LINE | `AuthLineRequest` | |
| OKTA | `AuthOktaRequest` | 需 `domainPrefix`、`authServerId` |
| PROGINN（程序员客栈） | `AuthProginnRequest` | |
| AFDIAN（爱发电） | `AuthAfDianRequest` | |
| APPLE | `AuthAppleRequest` | ES256 JWT 签名 + id_token 校验，需引入加密库（BouncyCastle 或 net8 原生 ECDsa，三框架都要能编译） |
| WECHAT_MINI_PROGRAM | `AuthWechatMiniProgramRequest` | |
| QQ_MINI_PROGRAM | `AuthQQMiniProgramRequest` | |
| （支付宝证书模式） | `AuthAlipayCertRequest` | 不是独立平台名，是 ALIPAY 的证书变体；走支付宝 SDK，只能比对 authorize |

新增平台时：C# 枚举 `DefaultAuthSourceEnum` 用 JustAuth 原名，新建 `Config/AuthSources/<X>AuthSource.cs` 与 `Request/AuthRequests/<X>AuthRequest.cs`，两个示例工厂（`Come.AspNetCore.Sample/AuthRequestFactory.cs`、`Come.Web.Sample/AuthRequestFactory.cs`）补 `case`。

---

## 3. 代码结构与关键基础设施

```
Come.CollectiveOAuth/                 类库（net462 / netstandard2.0 / net8.0）
  Config/ClientConfig.cs              对应 AuthConfig，全部字段已补齐（scopes、pkce、httpConfig、ignoreCheckState、tenantId、kid、teamId…）
  Config/HttpConfig.cs                超时（毫秒）+ 代理
  Config/AuthSources/*.cs             各平台地址；Java 未实现的 revoke/refresh 抛 AuthException(UNSUPPORTED)
  Request/DefaultAuthRequest.cs       对应 AuthDefaultRequest（构造时校验配置、login、getScopes、responseError）
  Request/AuthRequests/*.cs           各平台实现
  Exceptions/AuthException.cs         对应 AuthException（errorCode/errorMsg；Message 为 null 时保持 null）
  Cache/                              DefaultAuthStateCache（3 分钟绝对过期 + 定时清理）、DistributedAuthStateCache（IDistributedCache）
  Enums/Scope/*.cs                    30 个 Scope 类，由 tools/gen_scopes.py 从 Java 源码生成，勿手改
  Utils/JsonExtensions.cs             fastjson 语义读取 JObject（getString 缺失返回 null、getIntValue 默认 0…）
  Utils/GlobalAuthUtil.cs             GlobalAuthUtils 等价方法：urlEncode(Java 语义)、parseStringToMap、parseMapToString、各平台签名、md5…
  Utils/HttpUtils.cs                  共享 HttpClient；默认超时 3000ms、默认 UA 同 simple-http；非 2xx 抛异常；internal Sender 可注入
  Utils/UrlBuilder.cs                 build() 默认编码参数值（有意差异）
  Utils/AuthChecker.cs、AuthSourceNames.cs（C# ↔ JustAuth 平台名映射）、PkceUtil、UuidUtils、Base64Utils、RandomUtil、AuthScopeUtils
Come.CollectiveOAuth.Tests/           xUnit（net8.0）
  Fixtures/<JustAuth名>.json          差分输入
  Golden/<JustAuth名>.json            Java 生成的标准答案（只能由工具生成，禁止手改）
  GoldenTests.cs、Support/GoldenNormalizer.cs、Support/GoldenRequestFactory.cs（平台登记表）
tools/justauth-golden/                Java 标准答案生成器（Maven，依赖 JustAuth 1.16.7）
tools/gen_scopes.py                   Scope 生成脚本
Come.AspNetCore.Sample/、Come.Web.Sample/   示例项目
```

**C# 与 JustAuth 平台名不同的 5 个**（`AuthSourceNames` 负责换算）：
`XIAOMI`=`MI`，`ALIPAY_MP`=`ALIPAY`，`DINGTALK_SCAN`=`DINGTALK`，`WECHAT_ENTERPRISE`=`WECHAT_ENTERPRISE_WEB`，`WECHAT_ENTERPRISE_SCAN`=`WECHAT_ENTERPRISE`（注意最后两个是交叉的）。

---

## 4. 差分测试（核心验证手段）

**原理**：Java 端通过 simple-http 的 `HttpUtil.setHttp` 注入回放桩，运行 JustAuth，记录「授权地址 / 发出的请求 / 返回结果」写入 golden；C# 端通过 `HttpUtils.Sender` 注入同样的回放桩，执行相同输入后与 golden 比对。

**Fixture 格式**：
```json
{
  "source": "GITHUB",
  "config": { "clientId": "cid", "clientSecret": "csecret", "redirectUri": "https://www.justauth.cn/callback/github" },
  "state": "fixedstate000000000000000000000",
  "ignore": { "params": ["oauth_nonce", "metas.timestamp"] },
  "authorizeResponses": [],
  "scenarios": [
    { "name": "login_success", "action": "login", "callback": { "code": "c", "state": "fixedstate000000000000000000000" }, "responses": ["...", "..."] },
    { "name": "refresh_success", "action": "refresh", "token": { "refreshToken": "r" }, "responses": ["..."] }
  ]
}
```
- `action`：`login` / `refresh` / `revoke`。
- `redirectUri` 不得含 `?`、`&`（Java 版不编码，双方 URL 无法比较）。
- `ignore.params`：随机或时间相关的参数只比较是否存在。适用于 URL 参数、表单、请求头、OAuth1 头内参数、JSON 请求体；在 JSON 体里写字段名表示任意层级匹配，写点分路径（如 `metas.timestamp`）表示只匹配该位置。
- `authorizeResponses`：可选，生成授权地址时也要发请求的平台（如 Twitter）使用。

**比对规则**（`GoldenNormalizer`）：URL 拆成 base + 解码后的参数集合（忽略顺序）；表单体按参数集合比；JSON 体按 JSON 树比，数字统一为 decimal，删除值为 null 的属性；请求头只比双方显式传入的，忽略 `Content-Type/Host/User-Agent`；结果中 C# 独有的 `originalUser/originalUserStr` 不参与比较；平台名经 `AuthSourceNames` 换算。

**命令**（Windows + Git Bash）：
```bash
# 生成 golden（会重新生成全部平台）
cd tools/justauth-golden
mvn -q compile exec:java -Dexec.args="../../Come.CollectiveOAuth.Tests/Fixtures ../../Come.CollectiveOAuth.Tests/Golden"
# 测试与构建
dotnet test Come.CollectiveOAuth.Tests
dotnet build Come.CollectiveOAuth/Come.CollectiveOAuth.csproj
```
注意：重新生成后，DINGTALK、ELEME、HUAWEI、TWITTER 的 golden 会因时间戳或随机数变化（比对时已忽略），其余平台只有换行符变化。只提交自己改动平台的 golden，其它用 `git checkout -- Come.CollectiveOAuth.Tests/Golden` 还原。

**单个平台的移植流程**（详见阶段 2 计划）：读 Java → 对齐 AuthSource 地址 → 写 fixture（成功、平台错误，以及 Java 实现了的 refresh/revoke）→ 生成 golden → 在 `GoldenRequestFactory` 登记 → 看到红灯 → 逐行对照 Java 改写 C# → 全量测试通过 → 提交。

**Java → C# 对照要点**：
- `JSONObject.parseObject` 对应 `JsonExtensions.parseObject`；`obj.getString/getIntValue/...` 对应同名扩展方法。
- `new HttpUtils(cfg).get(url)` 对应 `HttpUtils.RequestGet(url, null, config.httpConfig)`；`.post(url, map, ...)` 对应 `RequestFormPost(url, GlobalAuthUtil.parseMapToString(map, true), header, null, config.httpConfig)`。
- `StringUtils.isEmpty` 对应 `string.IsNullOrEmpty`（不是 `IsNullOrWhiteSpace`）；`TreeMap` 对应 `SortedDictionary<string,…>(StringComparer.Ordinal)`。
- **URL 编码陷阱**：Java 先手动 `urlEncode` 或调用 `getScopes(sep, true, …)`、再交给 `UrlBuilder.build()` 的值，C# 一律传原值，因为 C# 的 `build()` 默认会编码。`#wechat_redirect` 这类片段拼在 `build()` 之后。
- 用户对象要设置 `rawUserInfo = obj`，同时保留兼容字段 `originalUser`、`originalUserStr`。

---

## 5. 与 JustAuth 的有意差异（必须保留）

1. state 校验通过后立即删除，防止重放（Java 不删）。
2. `UrlBuilder.build()` 和表单参数默认 URL 编码（Java 默认不编码）。
3. HTTP 非 2xx 抛异常，信息含状态码和截断到 500 字的响应体，不含 URL，以免泄露 secret。
4. 提供 `DistributedAuthStateCache`，多实例部署时可换成 Redis。
5. 保留 C# 命名（第 3 节的 5 个平台名、`FackbookAuthRequest`、`TwitteAuthRequest`）；`AuthResponse.data` 为 `object`。
6. `getScopes` 兼容旧配置：`scopes` 为空且配置了 `scope` 字符串时，直接使用该字符串。
7. 支付宝 C# SDK 不支持代理，Java 带代理参数的构造函数无法移植。

## 6. 相对旧版 C# 的行为变化（为对齐 Java 而改）

- 默认 HTTP 超时从 100 秒改为 **3 秒**（可用 `httpConfig.timeout` 调整）。
- 默认 state 缓存从 2 小时（访问续期）改为 **3 分钟**（到点过期）。
- 配置不全时，创建 Request 对象就抛 `AuthException(5002)`；回调地址不合法抛 5006。
- 未实现的 `refresh`/`revoke` 抛 `AuthException(5001)`，不再返回响应对象。
- `AuthUser.gender` 改为可空，未设置时为 null（原来默认 FEMALE）。
- `AuthToken.oauthCallbackConfirmed` 改为 `bool?`；`AuthCallback.code` 为空时回退到 `auth_code`。
- `urlEncode` 改为 Java 语义：大写十六进制，空格编码为 `%20`。
- state 改为 32 位小写十六进制 UUID。
- GitHub 默认 scope 为空（JustAuth 没有默认 scope）。
- 支付宝、微信开放平台、钉钉、美团、饿了么的 scope 固定为 Java 的值；抖音、华为、酷家乐改走 `getScopes`。
- LinkedIn 的 `refresh` 被删除（Java 未实现）；Pinterest 的错误检查按 Java 原样“永不报错”。
- 钉钉改为直接发 HTTP 请求，`TopSdk.dll` 已无代码使用（csproj 仍保留引用）。

## 7. 待用户决定的问题（未答复前保持与 Java 一致）

1. 第 6 节里那几个平台的 scope 是否要恢复为可配置？
2. LinkedIn 的 `refresh` 是否保留 C# 原实现？
3. Pinterest 是否有意偏离 Java，恢复错误检查？
4. 是否移除 `TopSdk.dll` 引用？

## 8. 已知限制

- 支付宝（含证书模式）通过官方 SDK 发请求，差分只能比对授权地址；登录和刷新靠人工对照。已核对的 10 项：
  1. 构造函数：新增 `(config, alipayPublicKey[, cache])`，原构造函数取 `config.alipayPublicKey`。
  2. 校验顺序：基类校验 clientId/secret 和回调协议，然后校验公钥非空（5002），最后校验回调不能是 localhost（5006）。
  3. SDK 客户端参数：网关、clientId、clientSecret（私钥）、json、UTF-8、公钥、RSA2。
  4. checkCode 校验 `auth_code`。
  5. getAccessToken：SDK 异常包装为 `AuthException(e)`（错误码 0），`IsError` 时抛 `AuthException(SubMsg)`。
  6. refresh：异常直接抛出。
  7. getUserInfo：只捕获 `AopException`；location 为“省 市”；username 为空时取 nickName；性别用 `getRealGender`。
  8. rawUserInfo 按 fastjson 规则生成：驼峰字段名、省略 null。
  9. authorize：scope 固定为 `auth_user`。
  10. AuthSource：revoke/refresh 抛 `UNSUPPORTED`。
- 值为 null 的表单参数未纳入比对：fastjson 序列化会丢弃它，而各 Java HTTP 实现的处理也不一致。
- Twitter 的 `login_invalid_verifier` 场景返回 2000，这是 JustAuth 原行为（不校验 access_token 响应），C# 已照搬。
- MVC 示例只验证了 net462 下能编译；本机缺 VS Web 构建组件，无法整体构建或运行。

---

## 9. 环境与操作注意（很重要）

- **系统**：Windows 10，Shell 为 Git Bash；.NET SDK 6 和 10，运行时 6、8.0.31、10；Java 8，Maven 3.6.1；Python 3.12。
- **JustAuth 源码**：克隆在 `/tmp/JustAuth`，丢失后可重新获取：`git clone --depth 1 https://github.com/justauth/JustAuth.git /tmp/JustAuth`。Windows 版 python/java 不认识 `/tmp`，要用 `cygpath -w /tmp/JustAuth` 转换路径。
- **换行符**：仓库 `core.autocrlf=true`，工作区是 CRLF。**不要对已有文件用 `sed -i`**（会把 CRLF 改成 LF），请用编辑工具或 Python 保留换行。新文件尽量用 CRLF、不带 BOM。
- **敏感文件**：`Come.Web.Sample/Web.config` 和 `Come.AspNetCore.Sample/appsettings.Development.json` 有用户本地未提交的改动，**Web.config 里含真实的支付宝私钥**。永远不要提交或覆盖这两个文件，`git add` 时要显式排除。
- **并行子代理与 worktree**：Claude Code 自动创建的 worktree 基于 `master`，不是当前分支。必须手动从 `feature/justauth-port` 创建（例如 `git worktree add -b batch-x ../CollectiveOAuth-wt/batch-x feature/justauth-port`），并让子代理先确认基线提交。仓库已忽略 `.claude/worktrees/`。
- **共享文件**由主控方修改，平台移植者不要动：`DefaultAuthRequest`、`Utils/*`、`Models/*`、`ClientConfig`、`Exceptions/*`、`GoldenNormalizer`、`GoldenTests`、`tools/justauth-golden/*`。发现缺口就报告，不要绕过。
- **提交规范**：中文提交信息，结尾加 `Co-Authored-By: <模型名> <noreply@anthropic.com>`（按所用工具调整）；只在用户要求时 push。

## 10. 建议的下一步

1. 若用户对第 7 节有答复，先处理。
2. 阶段 3：按第 2.2 节逐个新增平台，流程同阶段 2。可按批并行：钉钉/微信系、国内其它、海外、Apple + 支付宝证书单独一批（需加密库）。
3. 阶段 4：示例工厂补全全部平台；README 增加“与 JustAuth 的差异清单”（第 5、6 节）和平台对照表；视用户决定移除 TopSdk。
4. 完成后跑全量验证：`dotnet test`、三框架构建、Core 示例实际启动抽查授权跳转。
