# Unity 内置 C# 工具包装

本目录不是安装新的系统级 SDK，而是把当前 Unity Editor 自带的 C# 工具做成本地包装脚本，方便在终端里复用。

## 为什么不单独安装

当前机器上的 Unity Editor 已经自带以下工具：

- `dotnet`
- `csc`
- `mcs`
- `xbuild`

它们都位于当前项目使用的 Unity 版本目录下，只是没有进入系统全局 `PATH`。

## 当前包装脚本

- `unity-dotnet`
- `unity-csc`
- `unity-mcs`
- `unity-msbuild`
- `unity-license-warmup`
- `unity-license-diagnose`
- `unity-run-tests`

说明：

- `unity-msbuild` 当前会优先尝试真正的 `msbuild`，若 Unity 内未提供独立入口，则自动回退到 `xbuild`
- 这些脚本会优先读取 `ProjectSettings/ProjectVersion.txt` 中的版本号，自动匹配 `/Applications/Unity/Hub/Editor/<version>/Unity.app`

## 用法

```bash
Tools/unity-cli/unity-dotnet --info
Tools/unity-cli/unity-csc -help
Tools/unity-cli/unity-mcs --version
Tools/unity-cli/unity-msbuild /path/to/project.csproj
Tools/unity-cli/unity-license-warmup
Tools/unity-cli/unity-license-diagnose --use-temp-clone
Tools/unity-cli/unity-run-tests EditMode --assembly-names CampusRPG.Tests.EditMode
Tools/unity-cli/unity-run-tests PlayMode --assembly-names CampusRPG.Tests.PlayMode
Tools/unity-cli/unity-run-tests EditMode --group-filter '^CampusRPG\\.Tests\\.EditMode\\.'
Tools/unity-cli/unity-run-tests EditMode --group-filter '^CampusRPG\\.Tests\\.EditMode\\.Chapter01' --use-temp-clone
Tools/unity-cli/unity-run-tests EditMode --group-filter '^CampusRPG\\.Tests\\.EditMode\\.Chapter01' --startup-timeout 20
```

## 注意

- 这些工具只解决“终端里找不到编译器”的问题
- Unity 项目的最终编译结果仍以 Unity Editor 内部导入和编译为准
- 对 Unity 工程做静态编译检查时，仍可能需要补 Unity 引擎引用或以 Editor 生成的项目文件为准
- `unity-run-tests` 会主动拒绝在 `Unity Hub` 常驻的情况下执行
- `unity-license-warmup` 会先正常拉起一次 Unity 编辑器，等待 `[Project] Loading completed` 和授权恢复日志出现，再尝试软退出 `Unity Hub`；适合在 batchmode 又掉回 `0 entitlement groups` / `com.unity.editor.headless was not found` 之前，先把当前用户授权会话“热起来”
- 若 `unity-license-warmup` 本身超时，但 `Editor.log` 已明确出现 `[Project] Loading completed` 和 `Successfully updated access token`，可视为 GUI 侧预热已成功；这时继续确认 `Unity Hub` 已退出，再接 `unity-license-diagnose` / `unity-run-tests`
- `unity-license-diagnose` 用于先探测“当前 batchmode 授权是否已恢复”，适合在正式跑 `unity-run-tests` 前先做一次快速健康检查；它会同时输出本地 `UnityEntitlementLicense.xml` / `packageAccessControlList.xml` 摘要，以及 `Unity.Licensing.Client.log` 的最近关键证据，帮助区分“本地许可证缺项”还是“运行时授权解析会话失真”
- `unity-license-diagnose` 支持 `--override-user <name>`：会在启动 Unity 时覆盖 `USER`，用于验证 Licensing channel 是否跟环境身份绑定。如果通道从 `LicenseClient-don` 切到 `LicenseClient-codex`，且客户端开始处理 sandbox-home 里的 license，就说明当前问题确实和用户级授权通道绑定有关
- `unity-license-diagnose` 还支持 `--fresh-sandbox-home`：会创建一个全新的临时 HOME，但不复制原来的 `Library/Unity`、`UnityEntitlementLicense.xml`、PACL 或 `Unity.Licensing.Client.log`。如果这时再配合 `--override-user codex` 仍然出现 `readonly database` 和 `0 entitlement groups`，就说明问题已经不只是旧用户态文件污染
- `unity-license-diagnose` 现在还支持 `--override-hostname <name>`、`--hostaliases-local`、`--override-localdomain <value>`、`--override-res-options <value>`：可把 `HOSTNAME`、临时 `HOSTALIASES`、`LOCALDOMAIN`、`RES_OPTIONS` 一起带进 Unity 启动环境，用来验证“给 Unity 进程补主机名 / resolver 环境变量回退是否足够”
- `unity-license-diagnose` 现在还会输出 `POST_RUN_HOME_SUMMARY`：专门看这次 batchmode 结束后，临时 HOME 里到底新生成了什么。如果 fresh sandbox 下只有 `Unity.Licensing.Client.log` / `Unity.Entitlements.Audit.log`，但没有任何 license 或 PACL 文件，就说明 Unity 还没走到把许可证文件真正落盘的阶段
- `unity-license-diagnose` 现在还会输出 `CLIENT_FAILURE_SUMMARY`：把 `Unity.Licensing.Client.log` 里的关键失败行压缩成固定字段，例如远端 config 拉取失败、PACL 更新失败、`GetDomainName: -1`、`Token not found in cache`、`Processed 0 license files`。这样后续自动化不用再手翻大日志就能知道失败是落在更早期网络/身份/授权更新阶段
- `unity-license-diagnose` 现在还会输出 `SYSTEM_IDENTITY_SUMMARY`：给出当前机器的 `hostname`、`ComputerName`、`LocalHostName`、`HostName` 以及主机名查询结果。如果这里出现 `HostName: not set` 或 `AuthorizationCreate() failed: status = -60008`，同时客户端日志又有 `GetDomainName: -1`，就说明系统身份链路本身也是可疑项
- `unity-license-diagnose` 现在还会输出 `HOST_API_SUMMARY`：把 `hostname -f`、`scutil --get HostName`、`python socket.gethostname()`、`python socket.getfqdn()` 这些不同层的主机名 API 并排输出。若这里出现“`hostname -f` 正常、`socket.gethostname()` 正常，但 `scutil --get HostName` 失败、`socket.getfqdn()` 超时”，说明问题不是整机完全没有主机名，而是主机名/域名链路在不同 API 之间出现分裂
- `unity-license-diagnose` 现在还会输出 `RESOLVER_PATH_SUMMARY`：把 `/etc/hosts` 是否含本机名、当前 `search domain`、`domain: local` 的 reach 状态，以及“短主机名实际会被 `dscacheutil` 解析成什么”一起打印出来。若这里显示 `RESOLVER_SEARCH_DOMAIN: tongji.edu.cn`、`RESOLVER_LOCAL_REACH: ... Not Reachable`、`SHORT_HOST_LOOKUP_VALUE: dons-macbook-pro.tongji.edu.cn`，就说明短主机名已经偏离 `.local` 链路，跑去外部 search domain 了
- `unity-license-diagnose` 现在还会输出 `NETWORK_SCOPE_SUMMARY`：把 `State:/Network/Service/*/DNS` 里的服务级 DNS 配置、scoped resolver 的接口归属、活跃 `utun*` 接口，以及 `scutil --nwi` 是否正常一起压成固定字段。若这里显示“默认 domain 服务是 `tongji.edu.cn`，同时还有 `utun8` scoped resolver，且 `NWI_STATUS: failed`”，就说明当前不仅是主机名链路分裂，连系统网络作用域视图本身也值得怀疑
- `NETWORK_SCOPE_SUMMARY` 现在还会同时给出 `GLOBAL_DNS_SERVERS`、`PRIMARY_DOMAIN_SERVICE_SERVERS`、`PRIMARY_SCOPED_NAMESERVER`，以及 `SERVICE_GLOBAL_DNS_MATCH` / `SERVICE_SCOPED_DNS_MATCH`。若这里出现“主服务 DNS 还是 `202.120.190.*`，但全局 DNS 和 `en0` scoped resolver 实际都走 `223.6.6.6`”，就说明当前不仅有 search domain 漂移，还存在“服务配置”和“实际生效 resolver”不一致
- `NETWORK_SCOPE_SUMMARY` 现在还会给出 `GLOBAL_DNS_CONFIG_SERVICE_ID` 和 `GLOBAL_CONFIG_POINTS_TO_PRIMARY_SERVICE`。若这里显示“全局 DNS 配置归属仍指向主 domain 服务，但实际 `GLOBAL_DNS_SERVERS` 还是和主服务 DNS 不一致”，就说明不是“切到了别的 service id”，而更像是同一条服务下的 resolver 被额外改写或接管
- `NETWORK_SCOPE_SUMMARY` 现在还会给出 `VPN_DNS_SERVICE_ID`、`VPN_DNS_SERVICE_SERVERS`、`GLOBAL_DNS_MATCHES_VPN_SERVICE` 与 `SCOPED_DNS_MATCHES_VPN_SERVICE`。若这里显示“当前全局 DNS 和 `en0` scoped resolver 的 nameserver 都正好等于 `utun8` 这条 VPN 服务的 DNS”，就说明当前更像是 VPN/`utun` 侧在接管实际 resolver，而不是普通 DHCP 服务自己返回了这组 DNS
- `NETWORK_SCOPE_SUMMARY` 现在还会给出 `VPN_DNS_SERVICE_STATE_SERVER_ADDRESS`、`VPN_DNS_SERVICE_ROUTE_COUNT` 与 `VPN_SERVICE_HAS_LOOPBACK_SERVER`。若这里显示 VPN 服务的 `ServerAddress` 是 `127.0.0.1`，同时还有额外路由，就说明这条 `utun` 更像本地代理型 VPN/隧道，而不是一条普通网络接口
- `unity-license-diagnose` 现在还会输出 `ENV_FALLBACK_SUMMARY`：专门测试“给进程显式塞 `HOSTNAME` 或 `HOSTALIASES` 回退”能不能把 FQDN 路径救回来。若这里两个探针都还是 `timeout`，说明当前问题不是缺一个简单环境变量，而是更底层的主机名/解析链路状态异常
- 若再配合 `--override-hostname Dons-MacBook-Pro.local --hostaliases-local --override-localdomain local --override-res-options ndots:1` 做真实 Unity 启动，结果仍然保留 `CLIENT_COOKIE_DOMAIN_ERROR: GetDomainName: -1`、`BATCHMODE_HEADLESS_ENTITLEMENT: missing` 和 `0 free entitlements`，那就说明不只是 `HOSTNAME` / `HOSTALIASES`，连常见 resolver 环境变量回退在真实 batchmode 下也无效
- `unity-license-diagnose` 现在还会输出 `REMOTE_CONNECTIVITY_SUMMARY`：它会把 Unity Licensing Client 这次实际涉及到的远端地址提出来，对每个 URL 做 DNS 解析和 HTTPS 探测。若这里显示 `REMOTE_CONNECTIVITY_CONCLUSION: reachable`，但客户端仍报 `GetDomainName: -1` / `CookieContainer` 初始化异常，就说明问题不是普通断网，而是客户端在真正发出请求前就卡在本机身份初始化
- `unity-license-diagnose` 现在还会输出 `CORRELATION_SUMMARY`：把前面已经查明的组合条件直接收口成 yes/no 结论，例如 `CORRELATION_HOSTNAME_CHAIN_SUSPECT`、`CORRELATION_FRESH_HOME_STILL_ZERO_LICENSES`、`CORRELATION_EARLY_IDENTITY_OR_NETWORK_FAILURE`、`CORRELATION_NEW_USER_CHANNEL_STILL_READONLY`。这样后续自动化可以直接根据结论字段推进，而不是每次重新拼装整组证据
- `CORRELATION_SUMMARY` 现在还会继续补两条网络作用域结论：`CORRELATION_SCOPED_UTUN_PRESENT_WITH_SEARCH_DOMAIN` 用来标记“默认 search domain 存在，同时 scoped resolver 里也挂着 `utun` 接口”；`CORRELATION_NWI_FAILED_WHILE_UTUN_ACTIVE` 用来标记“`scutil --nwi` 失败时，系统里仍存在活跃 `utun` 网络作用域”
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_GLOBAL_DNS_DIFFERS_FROM_PRIMARY_SERVICE`。当 `State:/Network/Global/DNS` 的 nameserver 已和主 domain 服务的 nameserver 脱钩时，这个字段会变成 `yes`，帮助自动化继续把重点放在系统 resolver / 网络作用域漂移，而不是 Unity 许可证文件本身
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_GLOBAL_CONFIG_POINTS_TO_PRIMARY_BUT_DNS_DIFFERS`。当全局 DNS 逻辑上仍归属于主 domain 服务，但 nameserver 已经和主服务自报值不同，这个字段会变成 `yes`，帮助自动化把重点继续放在“同服务内 DNS 被改写”的系统链路，而不是误判成切换了网络服务
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_EFFECTIVE_DNS_MATCHES_VPN_SERVICE`。当全局 DNS 和首个 scoped resolver 的实际 nameserver 都与 `ConfigMethod=VPN` 的那条服务完全一致时，这个字段会变成 `yes`，帮助自动化继续把重点放在 VPN / `utun` / 网络作用域接管链，而不是 Unity 本地许可证文件
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_LOOPBACK_VPN_SERVICE_MATCHES_EFFECTIVE_DNS`。当当前有效 DNS 不仅匹配 VPN 服务，而且这条 VPN 服务本身还通过 `127.0.0.1` 这类 loopback 地址提供本地代理入口时，这个字段会变成 `yes`，进一步说明排查应优先落在 VPN / 本地代理 / resolver 接管链
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_REMOTE_ENDPOINTS_REACHABLE_BUT_CLIENT_INIT_FAILED`。当 Unity 的远端 config / PACL 地址在当前终端里都能正常解析和返回，但客户端日志依然稳定出现 `GetDomainName: -1` 时，这个字段会变成 `yes`，帮助自动化把重点继续放在主机名/域名链路，而不是继续排查普通网络连通性
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_HOSTNAME_API_SPLIT_BRAIN`。当 `HostName` 查询失败，但 `hostname -f` 仍能返回本机名，或 `python socket.getfqdn()` 反而超时，就说明当前不是“整个主机名都丢了”，而是不同主机名 API 看到的是不一致状态；这和 `GetDomainName: -1` 属于同一类更早期身份链异常
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_SHORT_HOSTNAME_EXPANDS_TO_SEARCH_DOMAIN` 和 `CORRELATION_LOCAL_MDNS_UNREACHABLE_AND_SHORTNAME_DRIFTS`。前者表示短主机名被直接扩成外部 search domain；后者表示 `.local` 的 mDNS resolver 本身还处于 `Not Reachable`，而短主机名又确实漂到了外部域名。这组组合比单独的 `HostName not set` 更接近 Unity 客户端 `GetDomainName: -1` 的直接触发条件
- `CORRELATION_SUMMARY` 现在还新增了 `CORRELATION_ENV_HOST_FALLBACKS_DONT_UNBLOCK_FQDN`。当 `HOSTNAME` 和 `HOSTALIASES` 两条最常见的进程级回退都不能让 FQDN 探针恢复时，这个字段会变成 `yes`，表示后续不该再优先把精力放在 Unity 启动环境变量补丁上
- 若当前 shell 连进程列表都拿不到，`unity-license-diagnose` 会把这件事明确标成 `process-inspection-unavailable`，避免把系统层限制误判成 Unity 自身日志
- `unity-license-diagnose` 还会输出 `CoreBusinessMetrics.db` 及其目录/`wal`/`shm` 的可写状态；如果这里是 `WRITABLE=no`，基本就能解释 `attempt to write a readonly database`
- `unity-license-diagnose` 还会做一次轻量写入探针：若 `TMP_WRITE_PROBE: success` 但 `METRICS_WRITE_PROBE: failed`，说明当前自动化环境不是“全局不能写”，而是被限制在 Unity 的用户目录之外
- 若追加 `--sandbox-home`，工具会把 Unity 的用户目录切到 `/tmp` 下的临时 HOME；如果此时 `METRICS_WRITE_PROBE: success` 但 `readonly database` 仍然出现，说明问题不只是在原始 `~/Library/Application Support/Unity` 写权限
- `unity-license-diagnose` 现在还会输出 `RUNTIME_LOG_SUMMARY`：直接给出客户端日志和本次 batchmode 日志最后一次 entitlement 计数，以及 `BATCHMODE_READONLY_DATABASE`、`BATCHMODE_HEADLESS_ENTITLEMENT`、`SANDBOX_HOME_READONLY_REPRO`、`SANDBOX_HOME_ZERO_ENTITLEMENT_REPRO`、`SANDBOX_HOME_WRITABLE_BUT_READONLY` 等标准化字段，便于自动化直接判断阻塞是否仍然复现
- `unity-license-diagnose` 还会输出 `HOME_DB_INVENTORY`：列出当前 HOME 下 Unity 相关目录里实际可见的 `db/sqlite/lock` 文件数量。若 sandbox-home 已可写，但这里仍是 `0` 且 Unity 依旧报 `readonly database`，说明当前报错大概率不在临时 HOME 内这些常见数据库文件上
- `unity-license-diagnose` 还会输出 `LOG_CONTEXT_SUMMARY`：给出 batchmode 实际连接的 Licensing channel、notification channel、客户端最近处理过的 license 路径，以及 `readonly database` 日志是否自带路径提示。若 sandbox-home 下仍显示 `SANDBOX_HOME_SHARED_USER_CHANNEL: yes`，说明 Unity 仍连着按原用户命名的授权通道，而不是一条随临时 HOME 隔离的新通道
- 若同项目已有 Unity Editor 打开，`unity-run-tests` 会自动把工程复制到临时克隆目录后再跑批处理，避免 `Library` 与项目锁冲突
- 若你想主动隔离本次回归，可显式追加 `--use-temp-clone`；临时克隆默认创建在 `${TMPDIR:-/tmp}`，也可通过 `--clone-root <path>` 指定
- `unity-run-tests` 现在会在每次启动前删除旧的 `/tmp/*tests.log` 与结果 XML，避免启动监控误读上一次残留的 entitlement / readonly 错误日志；如果你看到“秒失败”现象，先确认是不是旧版脚本
- `unity-run-tests` 会在启动阶段监控日志；若 Unity 长时间没进入 `Package Manager` / `COMMAND LINE ARGUMENTS`，或已经出现 `0 entitlement groups`、`com.unity.editor.headless was not found`、`attempt to write a readonly database`，脚本会尽快中止并给出更明确的环境诊断
- 当 `unity-run-tests` 明确提示 entitlement / headless / readonly database 启动阻塞时，优先执行一次 `Tools/unity-cli/unity-license-warmup`，再重跑 `unity-license-diagnose` 或 `unity-run-tests`；本项目已重复验证过“先正常拉起 GUI 编辑器热授权，再退掉 Unity Hub”的恢复路径
- `unity-run-tests` 故意不传 `-quit`；当前 `com.unity.test-framework@1.6.0` 会在测试完成后自行退出，额外附带 `-quit` 会导致测试不启动也不产出结果文件
- `unity-run-tests` 的 `--group-filter` 会原样转发到 Unity `-testFilter`，更适合按命名空间 / Fixture 正则过滤，而不是按单个测试方法名过滤

## 已验证恢复链路

当 `unity-run-tests` 先前出现过 `0 entitlement groups` / `readonly database` 启动阻塞时，当前项目里已经验证过下面这条恢复顺序：

1. 先执行 `Tools/unity-cli/unity-license-warmup`
2. 若 warmup 命令自身超时，检查 `~/Library/Logs/Unity/Editor.log` 是否已有 `[Project] Loading completed` 与 `Successfully updated access token`
3. 确认 `Unity Hub` 已退出，避免 batchmode 被 Hub 常驻干扰
4. 用 `Tools/unity-cli/unity-license-diagnose --use-temp-clone` 复核 batchmode 是否已恢复到 `STATUS: startup-ok`
5. 再执行目标 `unity-run-tests` 命令

这条链路在 `2026-04-20` 已实际验证通过，并最终跑通了：

```bash
Tools/unity-cli/unity-run-tests EditMode --group-filter '^(CampusRPG\\.Tests\\.EditMode\\.(CameraObstacleResolverTests|CombatProxyVisualUtilityTests|CombatTestAnimationAssetWiringTests))$'
```

结果：`/tmp/TY_NEW_editmode_tests.xml` 为 `10/10` passed。
