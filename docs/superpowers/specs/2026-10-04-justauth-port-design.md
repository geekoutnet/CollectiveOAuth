# CollectiveOAuth 对齐 JustAuth 1.16.7 设计

## 目标

以 JustAuth 1.16.7（https://github.com/justauth/JustAuth ，提交 694bbf1）为基准，把它的全部功能移植到 CollectiveOAuth，
**功能行为一致**：平台、配置项、授权地址、发出的请求、响应解析结果与 Java 版相同；有意保留的差异见下文，并全部写入 README。

## 已确定的决策

| 决策 | 结论 |
|---|---|
| 一致的标准 | 功能行为与 1.16.7 一致，保留安全/健壮性修复 |
| 对外 API | 保留现有 C# API（`ClientConfig`、`IAuthRequest`、`XxxAuthRequest`、小写方法名、同步接口），只扩展 |
| 目标框架 | 类库 `net462;netstandard2.0;net8.0`；Core 示例升级到 net8.0，MVC 示例升级到 net462 |
| 验证方式 | Java 差分对照：Java 端运行 JustAuth 生成标准答案，C# 端同输入自动比对 |

## 有意保留的差异

1. state 校验通过后立即删除（Java 版不删除，可重放）。
2. `UrlBuilder.build()` 与表单参数默认 URL 编码（Java 版默认不编码，靠各平台手动 `urlEncode`）。差分测试比对 URL 时，双方都先解码成参数字典再比较。
3. HTTP 非 2xx 抛出异常，异常信息包含状态码和截断后的响应体。
4. 额外提供 `DistributedAuthStateCache`（基于 `IDistributedCache`）。
5. 命名沿用 C#：枚举 `XIAOMI`=`MI`、`ALIPAY_MP`=`ALIPAY`、`DINGTALK_SCAN`=`DINGTALK`、`WECHAT_ENTERPRISE`=`WECHAT_ENTERPRISE_WEB`、`WECHAT_ENTERPRISE_SCAN`=`WECHAT_ENTERPRISE`；新增平台使用 JustAuth 原名。
6. `AuthResponse.data` 为 `object`（Java 为泛型）。

## 阶段

| 阶段 | 内容 | 完成标准 |
|---|---|---|
| 0 | 提交上一轮修复（排除含密钥的本地配置） | 工作区仅剩本地配置改动 |
| 1 地基 | 框架升级；`ClientConfig` 补齐 24 个字段；`AuthException`；带过期清理的默认缓存；32 个 Scope 枚举与 `AuthScopeUtils`；PKCE；`ignoreCheckState`/`ignoreCheckRedirectUri`；`AuthChecker` 对齐；`HttpUtils` 可注入（测试）并支持代理 | 编译通过，JustAuth 原有单元测试移植后通过 |
| 1b 差分框架 | Java 工具（`tools/justauth-golden`）：通过 simple-http 的 `HttpUtil.setHttp` 注入录制/回放桩，对每个平台输出 `authorize` URL、请求序列、`login`/`refresh`/`revoke` 结果 JSON；C# 测试项目（`Come.CollectiveOAuth.Tests`）读取同一输入与回放响应，比对输出。nonce、时间戳、签名等随机字段归一化 | GitHub 平台全链路比对通过 |
| 2 对齐已有平台 | 现有 30 个平台逐个对齐 1.16.7，补齐 Twitter | 差分测试通过 |
| 3 新增平台 | 批次①国内：DINGTALK_V2、DINGTALK_ACCOUNT、CSDN、HUAWEI_V3、WECHAT_ENTERPRISE_V2、WECHAT_ENTERPRISE_QRCODE_THIRD、FEISHU、JD、ALIYUN、XMLY、PROGINN、AFDIAN、WECHAT_MINI_PROGRAM、QQ_MINI_PROGRAM；批次②海外：MICROSOFT_CN、AMAZON、SLACK、LINE、OKTA、FIGMA；批次③加密：APPLE、支付宝证书模式 | 每批差分测试通过 |
| 4 收尾 | 示例工厂覆盖全部平台；README 补充差异清单与平台对照表 | 文档完整 |

## 关键组件

- **配置**：`ClientConfig` 新增 JustAuth `AuthConfig` 的全部字段（`usertype`、`domainPrefix`、`httpConfig`、`ignoreCheckState`、`scopes`、`deviceId`、`clientOsType`、`packId`、`pkce`、`authServerId`、`ignoreCheckRedirectUri`、`tenantId`、`kid`、`teamId`、`loginType`、`lang`、`dingTalk*` 等）。已有字段类型保持兼容（如 `unionId` 维持 string，按“非空且为 true”解释）。
- **异常**：`AuthException(code, msg, source)`；`login` 捕获后转为 `AuthResponse(code, msg)`，与 Java 的 `responseError` 一致。
- **HTTP**：`HttpUtils` 保持静态 API，新增可替换的底层发送器（测试注入录制/回放），支持 `HttpConfig`（超时、代理）。
- **缓存**：默认缓存实现对齐 `AuthDefaultCache`（默认 3 分钟、定时清理过期项）。

## 风险

- 支付宝走官方 SDK，HTTP 无法拦截，只比对调用 SDK 的入参。
- 部分平台可能已下线，只保证与 JustAuth 代码逻辑一致。
- 范围约等于 JustAuth 主代码（约 130 个类），每个阶段结束后汇报。
