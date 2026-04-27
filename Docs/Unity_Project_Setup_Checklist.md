# Unity 工程初始化清单

本清单用于把当前模板工程整理成适合独立开发的 ARPG 工程基线。目标是尽快让工程进入“可持续开发”状态，而不是停留在模板试验阶段。

## 1. 引擎与管线基线

| 项目 | 当前状态 | 建议 |
|---|---|---|
| Unity 版本 | `6000.4.2f1` | 第一版锁定，不中途切小版本 |
| 渲染管线 | `Built-in Render Pipeline` | 第一版维持轻量内置管线，优先保证批处理稳定、素材兼容和跨平台可交付 |
| 输入方案 | `Input System` 已启用 | 保留并按项目动作表重建输入资产 |
| 目标平台 | Windows / Mac | 两端都做构建验证 |

## 2. Package 基线

当前工程已包含：

- `com.unity.cinemachine`
- `com.unity.inputsystem`
- `com.unity.ugui`
- `com.unity.timeline`

已清理的模板残留包：

- `com.unity.collab-proxy`
- `com.unity.multiplayer.center`
- `com.unity.visualscripting`

移除原则：

- 第一版不用的包尽量清理
- 但不要在第 1 周后半段频繁改包，避免无关风险

### 批处理与授权注意事项

- `macOS` 下执行 Unity `-batchmode`、`-runTests`、`-executeMethod` 前，先退出 `Unity Hub`。
- 原因不是“项目代码报错”，而是 `Unity Hub` 可能常驻旧版 `UnityLicensingClient`，与 `6000.4.2f1` 编辑器需要的授权协议版本不一致，直接阻断自动化链路。
- 若主工程已被本地 Unity 打开，批处理优先在临时克隆目录执行，避免 `Library` 与项目锁冲突；`Tools/unity-cli/unity-run-tests` 已支持自动回退到临时克隆。
- `Tools/unity-cli/unity-run-tests` 现会在启动阶段监控日志；如果 Unity 一直没进入测试初始化，且日志已经出现 `0 entitlement groups`、`com.unity.editor.headless was not found`、`attempt to write a readonly database`，脚本会提前中止并把问题归类为环境阻塞，避免自动化长时间挂死。
- 当 `unity-run-tests` 或 `unity-license-diagnose` 继续报 `0 entitlement groups` / `com.unity.editor.headless was not found` 时，优先先跑一次 `Tools/unity-cli/unity-license-warmup`：它会正常拉起 GUI 编辑器热授权，等项目加载和授权恢复日志出现后再尝试软退出 `Unity Hub`，把当前用户会话拉回更适合继续跑 batchmode 的状态。
- 当前若只是想确认“batchmode 授权有没有恢复”，优先先跑 `Tools/unity-cli/unity-license-diagnose --use-temp-clone`；它会直接输出 `startup-ok`、`entitlement-missing`、`headless-entitlement-missing`、`readonly-database` 等状态，并附带本地 `UnityEntitlementLicense.xml` / `packageAccessControlList.xml` 摘要、`Unity.Licensing.Client.log` 最近证据与本次 batchmode 关键日志，避免一上来就把时间耗在完整测试命令上。
- 若当前终端环境本身不允许进程探测，`unity-license-diagnose` 会明确输出 `process-inspection-unavailable`；这表示“当前看不到进程列表”，不表示 Unity 进程一定不存在。
- 若输出里的 `METRICS_DB_EVIDENCE` 显示 `CoreBusinessMetrics.db`、`CoreBusinessMetrics.db-wal`、`CoreBusinessMetrics.db-shm` 或其目录是 `WRITABLE=no`，优先按 `readonly database` 环境阻塞处理，而不是继续怀疑章节代码。
- 若同时看到 `TMP_WRITE_PROBE: success` 但 `METRICS_WRITE_PROBE: failed`，说明当前自动化环境仍能写临时目录，但不能写 Unity 的用户数据目录；这更像沙箱/执行环境限制，不是普通文件权限位问题。
- 若追加 `--sandbox-home` 后，`METRICS_WRITE_PROBE` 已变成 `success`，但日志里仍有 `attempt to write a readonly database`，说明问题不只在原始 `~/Library/Application Support/Unity` 写权限，还要继续查 Unity/授权客户端当前会话里访问的其他数据库或状态源。
- 若 `RUNTIME_LOG_SUMMARY` 同时出现 `CLIENT_LAST_FREE_ENTITLEMENTS` 仍大于 `0`、但 `BATCHMODE_LAST_FREE_ENTITLEMENTS: 0`，说明本地授权客户端最近一次响应和当前 batchmode 会话并不一致；这时更应优先怀疑运行时授权解析会话，而不是章节代码。
- 若 `SANDBOX_HOME_WRITABLE_BUT_READONLY: yes`，说明即使 Unity 用户目录已经切到可写的临时 HOME，`readonly database` 依旧存在；这可以直接排除“只要修原始 `~/Library/Application Support/Unity` 权限就够了”的假设。
- 若 sandbox-home 下 `HOME_DB_FILE_COUNT: 0`，同时 `SANDBOX_HOME_WRITABLE_BUT_READONLY: yes` 仍成立，说明临时 HOME 里连常见的 `db/sqlite/lock` 文件都没有生成，但 Unity 依旧报只读数据库；这时不应继续只盯 `CoreBusinessMetrics.db`，而要怀疑 Unity / Licensing 在访问别的状态源。
- 若 `LOG_CONTEXT_SUMMARY` 里出现 `SANDBOX_HOME_SHARED_USER_CHANNEL: yes`，说明即使已经切到临时 HOME，batchmode 仍在连接 `LicenseClient-<用户名>` 这类用户级授权通道；这时更应优先怀疑外部 Licensing Client 会话或用户级状态，而不是把问题都归到当前临时 HOME。
- 若想直接验证“问题是不是绑在当前用户名通道上”，可执行 `Tools/unity-cli/unity-license-diagnose --use-temp-clone --sandbox-home --override-user codex`。若输出里的 channel 从 `LicenseClient-don` 变成 `LicenseClient-codex`，同时客户端开始处理 sandbox-home 里的 `UnityEntitlementLicense.xml`，说明 `USER` 才是切换授权通道的关键因子，而不是 `LOGNAME`。
- 若想进一步排除“是不是复制进来的旧 Unity 用户态文件把会话污染了”，可执行 `Tools/unity-cli/unity-license-diagnose --use-temp-clone --fresh-sandbox-home --override-user codex`。如果此时 `LOCAL_LICENSE: missing`、`CLIENT_LOG: missing`，但仍然出现 `LicenseClient-codex`、`readonly database` 和 `0 entitlement groups`，说明主要问题已经不在被复制进 sandbox-home 的旧文件上。
- 若想验证“给 Unity 进程补主机名 / resolver 环境变量是否足够”，可执行 `Tools/unity-cli/unity-license-diagnose --use-temp-clone --fresh-sandbox-home --override-user codex --override-hostname Dons-MacBook-Pro.local --hostaliases-local --override-localdomain local --override-res-options ndots:1`。如果这时仍然出现 `CLIENT_COOKIE_DOMAIN_ERROR: GetDomainName: -1`、`BATCHMODE_HEADLESS_ENTITLEMENT: missing`、`BATCHMODE_LAST_FREE_ENTITLEMENTS: 0`，说明这类启动环境回退在真实 Unity 启动里也无效。
- 若 fresh sandbox 运行后 `POST_RUN_CLIENT_LOG: present`、`POST_RUN_AUDIT_LOG: present`，但 `POST_RUN_LICENSE_FILE: missing`、`POST_RUN_PACL_FILE: missing` 且 `POST_RUN_LICENSE_FILE_COUNT: 0`，说明 Unity 已经开始写日志，但还没走到真正把许可证/PACL 文件落盘的阶段；这时更应优先怀疑授权更新流程本身被更早的外部状态卡住。
- 若 `CLIENT_FAILURE_SUMMARY` 同时出现 `CLIENT_REMOTE_CONFIG_ERROR`、`CLIENT_PACL_UPDATE_ERROR`、`CLIENT_COOKIE_DOMAIN_ERROR: GetDomainName: -1`、`CLIENT_TOKEN_CACHE_ERROR` 和 `CLIENT_ZERO_LICENSE_FILES`，说明这次失败已经落在“远端 config/PACL 更新 + 身份/缓存初始化 + 本地 license 文件数为 0”的更早阶段；这时不应再把主要精力放在已落盘许可证内容上。
- 若 `SYSTEM_IDENTITY_SUMMARY` 里同时看到 `SYSTEM_HOSTNAME` / `SYSTEM_LOCAL_HOSTNAME` 正常，但 `SYSTEM_HOST_NAME_ERROR` 显示 `AuthorizationCreate() failed: status = -60008` 且 `HostName: not set`，同时 `CLIENT_COOKIE_DOMAIN_ERROR` 仍然是 `GetDomainName: -1`，说明机器的某一层系统身份配置本身就不完整；这时应把主机名/域名链路列为并行嫌疑，而不是只盯 Unity 自己的许可证文件。
- 若 `HOST_API_SUMMARY` 里出现 `HOST_API_HOSTNAME_F_STATUS: ok`、`HOST_API_PY_SOCKET_HOSTNAME_STATUS: ok`，但 `HOST_API_SCUTIL_HOSTNAME_STATUS: failed`、`HOST_API_PY_SOCKET_FQDN_STATUS: timeout`，说明当前不是“主机名整体不可用”，而是“短主机名/本机名还能拿到，但完整域名链路或 HostName API 断了”。这类差异比单独看到 `HostName: not set` 更接近 `GetDomainName: -1` 的触发条件。
- 若 `RESOLVER_PATH_SUMMARY` 里同时看到 `ETC_HOSTS_HOSTNAME_MATCHES: 0`、`RESOLVER_SEARCH_DOMAIN: tongji.edu.cn`、`RESOLVER_LOCAL_REACH: ... Not Reachable`，并且 `SHORT_HOST_LOOKUP_VALUE` 变成了 `dons-macbook-pro.tongji.edu.cn` 一类外部域名，说明短主机名已经没有优先落到 `.local` / 本机别名链路，而是被系统 search domain 扩展到外部 DNS 了。
- 若 `NETWORK_SCOPE_SUMMARY` 里看到 `PRIMARY_DOMAIN_SERVICE_DOMAIN: tongji.edu.cn`、`SCOPED_RESOLVER_2_INTERFACE: utun8`、`ACTIVE_UTUN_INTERFACES` 非空，且 `NWI_STATUS: failed`，说明问题已经不只是单条 resolver 记录异常，而是系统网络作用域本身也值得怀疑；这时应把 VPN / `utun` / scoped resolver 链路列为高优先级嫌疑。
- 若 `NETWORK_SCOPE_SUMMARY` 里同时出现 `GLOBAL_DNS_SERVERS: 223.6.6.6`、`PRIMARY_DOMAIN_SERVICE_SERVERS: 202.120.190.208,202.120.190.108`、`PRIMARY_SCOPED_NAMESERVER: 223.6.6.6`，并且 `SERVICE_GLOBAL_DNS_MATCH: no`、`SERVICE_SCOPED_DNS_MATCH: no`，说明“主服务上报的 DNS 服务器”和“系统实际生效 resolver”已经分裂；这时比单独看到 search domain 更值得优先排查。
- 若同一段里还出现 `GLOBAL_DNS_CONFIG_SERVICE_ID` 等于 `PRIMARY_DOMAIN_SERVICE_ID`，并且 `GLOBAL_CONFIG_POINTS_TO_PRIMARY_SERVICE: yes`，说明全局 DNS 逻辑上仍归属主 domain 服务，但 nameserver 已经被改写；这时应优先怀疑同一服务内的 resolver 漂移、接管或 VPN 注入，而不是误判成切到了另一条网络服务。
- 若同一段里再看到 `VPN_DNS_SERVICE_INTERFACE: utun8`、`VPN_DNS_SERVICE_SERVERS: 223.6.6.6`，并且 `GLOBAL_DNS_MATCHES_VPN_SERVICE: yes`、`SCOPED_DNS_MATCHES_VPN_SERVICE: yes`，说明当前实际生效的 DNS 已经和 VPN 服务完全对齐；这时应优先怀疑 VPN / `utun` / 网络作用域接管，而不是继续把注意力放在普通 DHCP DNS 或 Unity 本地文件上。
- 若这里还同时看到 `VPN_DNS_SERVICE_STATE_SERVER_ADDRESS: 127.0.0.1`、`VPN_DNS_SERVICE_ROUTE_COUNT` 大于 `0`，并且 `VPN_SERVICE_HAS_LOOPBACK_SERVER: yes`，说明这条 VPN 服务本身还带有本地代理入口和额外路由；这时更应优先怀疑本地代理型 VPN/隧道在接管 resolver，而不是普通网络服务配置偏移。
- 若 `ENV_FALLBACK_SUMMARY` 里 `ENV_HOSTNAME_OVERRIDE_FQDN_STATUS: timeout` 且 `ENV_HOSTALIASES_FQDN_STATUS: timeout`，说明即使给进程显式塞一个本机名回退，FQDN 路径也没有恢复；这时不应继续把希望放在简单环境变量补丁上。
- 若上面的真实 Unity 启动验证也失败，就可以把“Unity 进程缺少 `HOSTNAME` / `HOSTALIASES` / `LOCALDOMAIN` / `RES_OPTIONS`”整体降级为伪线索，后续应继续优先查系统 HostName、resolver、mDNS 或更底层身份链。
- 若 `REMOTE_CONNECTIVITY_SUMMARY` 对 `https://public-cdn.cloud.unity3d.com/config/production` 和 `https://license.unity3d.com/licenses/v1/packages/acl` 都给出 `REMOTE_DNS: ok` 且 `REMOTE_HTTP: ok`，但 `CLIENT_FAILURE_SUMMARY` 仍然出现 `CLIENT_COOKIE_DOMAIN_ERROR: GetDomainName: -1`，说明当前不是普通 DNS/HTTPS 不通，而是 Unity Licensing Client 在真正发出请求前就卡在 `CookieContainer` 初始化。
- 若 `CORRELATION_REMOTE_ENDPOINTS_REACHABLE_BUT_CLIENT_INIT_FAILED: yes`，可以把“外网完全不通”从主嫌疑里先降级；后续应优先围绕 `HostName` 未设置、`GetDomainName: -1`、以及客户端内部初始化链路继续调查。
- 若 `CORRELATION_HOSTNAME_API_SPLIT_BRAIN: yes`，说明当前更像是“不同主机名 API 读到的是两套状态”，而不是单纯某个 Unity 远端地址访问失败。后续应继续优先沿系统主机名配置和 FQDN 生成链路调查。
- 若 `CORRELATION_SHORT_HOSTNAME_EXPANDS_TO_SEARCH_DOMAIN: yes` 且 `CORRELATION_LOCAL_MDNS_UNREACHABLE_AND_SHORTNAME_DRIFTS: yes`，说明当前更像是“短主机名被错误送去外部 search domain，而 `.local` 路径本身又不可达”。这时应该把 resolver / mDNS 路径视为比普通网络和 Unity 许可证文件更高优先级的嫌疑。
- 若 `CORRELATION_ENV_HOST_FALLBACKS_DONT_UNBLOCK_FQDN: yes`，说明 `HOSTNAME` / `HOSTALIASES` 这类进程级回退没有实际缓解效果。后续应继续优先调查系统 HostName、resolver、mDNS 或更底层身份链，而不是继续堆 Unity 启动环境变量。
- 若 `CORRELATION_SCOPED_UTUN_PRESENT_WITH_SEARCH_DOMAIN: yes` 且 `CORRELATION_NWI_FAILED_WHILE_UTUN_ACTIVE: yes`，说明默认 search domain 与活跃 `utun` 网络作用域同时存在，而且系统 `nwi` 视图还拿不到正常状态；后续应继续沿系统网络作用域 / VPN / resolver 配置链调查，而不是回头重复验证外网 HTTP 连通性。
- 若 `CORRELATION_GLOBAL_DNS_DIFFERS_FROM_PRIMARY_SERVICE: yes`，说明 `State:/Network/Global/DNS` 已经和主 domain 服务的 DNS 服务器脱钩；后续应优先调查系统 resolver 漂移、网络作用域接管或 VPN 注入，而不是继续把重点放在 Unity 本地许可证文件。
- 若 `CORRELATION_GLOBAL_CONFIG_POINTS_TO_PRIMARY_BUT_DNS_DIFFERS: yes`，说明不仅 nameserver 脱钩，而且“全局 DNS 归属的 service id”仍然就是主 domain 服务；后续应继续优先调查同一服务上的 DNS 改写/接管链，而不是把重点放在 service 切换或 Unity 本地文件上。
- 若 `CORRELATION_EFFECTIVE_DNS_MATCHES_VPN_SERVICE: yes`，说明当前全局 DNS 与首个 scoped resolver 的实际 nameserver 都正好等于 VPN 服务自己的 DNS；后续应继续优先沿 VPN / `utun` / resolver 接管链调查，而不是回头重复验证普通外网连通性或 Unity 本地许可证文件。
- 若 `CORRELATION_LOOPBACK_VPN_SERVICE_MATCHES_EFFECTIVE_DNS: yes`，说明当前有效 DNS 不仅匹配 VPN 服务，而且这条 VPN 服务本身还通过 `127.0.0.1` 这类 loopback 地址提供本地入口；后续应继续优先沿本地代理型 VPN / `utun` / resolver 接管链调查，而不是继续把主要精力放在 Unity 本地授权文件。
- 若 `CORRELATION_SUMMARY` 同时出现 `CORRELATION_HOSTNAME_CHAIN_SUSPECT: yes`、`CORRELATION_FRESH_HOME_STILL_ZERO_LICENSES: yes`、`CORRELATION_EARLY_IDENTITY_OR_NETWORK_FAILURE: yes`、`CORRELATION_NEW_USER_CHANNEL_STILL_READONLY: yes`，说明这轮阻塞已经足够收敛：新用户通道已生效、旧 HOME 污染基本排除、许可证文件仍为 0、而更早期身份/网络链路依然失败。此时下一轮应优先沿系统身份和远端配置/PACL 获取链路继续查，而不是回头重复检查章节代码或已落盘文件。
- 若 `BATCHMODE_READONLY_PATH_HINT: none`，说明 `attempt to write a readonly database` 这一行本身没有给出具体文件路径；后续定位时应更多依赖 channel、license path 和额外文件探针，而不是期待 Unity 自己把目标数据库路径打出来。
- 当前工程使用的 `com.unity.test-framework@1.6.0` 下，命令行 `-runTests` 不要和 `-quit` 组合；测试框架会在结束后自动退出，额外附带 `-quit` 反而会让测试不启动且不产出结果文件。
- 本地批处理回归优先使用 `Tools/unity-cli/unity-run-tests`，统一 `EditMode` / `PlayMode` 的参数入口，减少误判。

### Unity 初始化问题排查顺序

若 Unity 出现“打开后一闪而退”或批处理日志反复出现以下关键字：

- `Licensing initialization failed`
- `Failed to handshake`
- `The connection with the Unity Licensing Client has been lost`
- `Channel LicenseClient-... doesn't exist`

按下面顺序处理：

1. 先确认是否有其他 Unity Editor 正在打开同一个工程。
2. 完全退出 `Unity Hub`，不要只关闭窗口。
3. 清理残留 `Unity.Licensing.Client` 进程，避免旧授权客户端继续占用通信通道。
4. 手动重新打开 `/Users/don/TY_NEW` 一次，先确认 GUI 编辑器能稳定停留。
5. 只有在编辑器能稳定启动后，再执行 `-batchmode`、`-runTests` 或 `-executeMethod`。
6. 若主工程正在人工编辑，自动化一律优先使用临时克隆目录执行。

判断原则：

- 若日志停在授权握手或授权重连，优先按环境问题处理，不要直接判断为运行时代码回归。
- 若 Unity 已成功进入测试执行阶段，再根据失败的具体测试结果判断是否属于代码问题。

本项目已在 `2026-04-16` 验证过该处理路径：清理残留授权进程后，真实 Unity 回归恢复到 `EditMode 15/15`、`PlayMode 4/4` 全通过。

## 3. Scene 基线

### 当前已创建

- `Assets/_Game/Scenes/Bootstrap.unity`
- `Assets/_Game/Scenes/MainMenu.unity`
- `Assets/_Game/Scenes/CombatTest.unity`
- `Assets/_Game/Scenes/BossTest.unity`
- `Assets/_Game/Scenes/Chapter01_Combined.unity`

### Build Settings 顺序建议

当前发布候选顺序以正式入口为第一位：

1. `MainMenu.unity`
2. `Chapter01_Combined.unity`
3. `Bootstrap.unity`
4. `CombatTest.unity`
5. `BossTest.unity`

发布候选构建入口统一走 `CampusRPG/Build/Validate Release Candidate Build Inputs`、`CampusRPG/Build/Build macOS Release Candidate` 或 `CampusRPG/Build/Build Windows Release Candidate`。终端自动化入口统一走 `Tools/unity-cli/ty-new-build-release validate|mac|windows --use-temp-clone`，确保 `-executeMethod` 外层有墙钟上限、独立 log 和临时克隆保护。输出固定在 `.gitignore` 已忽略的 `Builds/ReleaseCandidate/` 下。

### 当前模板内容处理建议

- `Assets/OutdoorsScene.unity` 仅作参考模板，不要继续在其上堆正式内容
- 正式生产统一迁入 `Assets/_Game/Scenes/`
- 当前这 5 个正式 Scene 是从模板场景复制出的占位基础版，后续应在 Unity 内分别整理内容与 Lighting
- `MainMenu.unity` 是当前发布候选第一入口；`Chapter01_Combined.unity` 保持第二场景，供继续第一章和运行时 smoke 直接加载
- `Chapter01_Combined.unity` 当前灰盒已包含 `Area03` 室内锁门清怪段，并用清场结果放出 `GateSigil`
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `AreaEntryView`，会在玩家进入入口、庭院、校舍内部与 Boss 区时给出一次短时到达提示，帮助玩家快速确认自己到了哪一段
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `CheckpointActivationView`，会在玩家踩到 `CP01/CP02/CP03` 时给出短提示，明确告知复活点已经前移
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `KeyItemAcquisitionView`，会在拿到 `GateSigil` 与 `RitualCore` 时给出短提示，明确告诉玩家门禁或章节推进已经发生
- `Chapter01_Combined.unity` 现已在 `Pickup_RitualCore` 上接入 `KeyItemBeaconView`，会在守门者倒地后给术式核心补一个世界空间引导标记，并在首次显现时追加一次短时地面扩散闪光，帮助玩家更快把视线落到章节终点
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `EncounterSealView`，会在入口教学、庭院与内场普通遭遇战开始时给出短提示，明确告诉玩家这波已封锁、需要先清怪
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `EncounterClearView`，会在入口教学、庭院与内场遭遇战清场后给出短提示，明确告诉玩家“这一波已经打完，可以继续推进了”
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `ChapterRouteBlockHintView`，并在四段章节门前布置阻塞提示触发器，玩家撞到未解锁路线时会直接得到“该清怪、拿印记还是先打 Boss”的解释
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `ChapterObjectiveView`，会根据当前区域、`GateSigil` 与守门者击败状态给出持续目标提示
- `Chapter01_Combined.unity` 现已在 `ChapterFlow` 上接入 `ChapterTutorialHintView`，会在入口教学区按“移动 -> 锁定 -> 攻击 -> 防守”顺序给出最小上手提示
- `Chapter01_Combined.unity` 现已在 `BossPresentationRig` 上接入 `BossCombatHintView`，会在守门者战开场时给出一次简短的“格挡近身连段、闪避大范围招式、抓后摇反打”提示，帮助玩家把前面学到的解法带进最终考核
- `Chapter01_Combined.unity` 中的 `ChapterCompleteView` 现会在拾取 `RitualCore` 后给出更明确的章节收尾总结，直接说明守门者已击败、术式核心已取得，以及当前章节自动存档已经更新
- HDRP 模板残留已经从运行时包与项目管线设置中移除；后续如需更换美术基线，优先通过材质与后处理轻量补足，不再回切 HDRP

## 4. Input Actions 规划

当前已创建正式输入资产：

- `Assets/_Game/Data/Input/CampusInputActions.inputactions`

建议 Action Maps：

### `Player`

| Action | 类型 | 默认键位 |
|---|---|---|
| Move | Value/Vector2 | `WASD` |
| Look | Value/Vector2 | `Mouse Delta` |
| LightAttack | Button | `LMB` |
| HeavyAttack | Button | `RMB` |
| Block | Button | `Left Ctrl` |
| Dodge | Button | `Left Shift` |
| Jump | Button | `Space` |
| Skill1 | Button | `Q` |
| Skill2 | Button | `E` |
| LockOn | Button | `Tab` |
| Interact | Button | `F` |
| Pause | Button | `Esc` |

### `UI`

| Action | 类型 | 默认键位 |
|---|---|---|
| Navigate | Value/Vector2 | 键盘方向 / WASD |
| Submit | Button | `Enter` |
| Cancel | Button | `Esc` |

说明：

- 当前模板自带 `Assets/InputSystem_Actions.inputactions`，现阶段已保留作参考，但正式输入配置已切换到 `CampusInputActions`
- 正式输入应与战斗动作语义一致，避免后期维护混乱

## 5. Layer 与 Tag 建议

### Layer

- `Player`
- `Enemy`
- `PlayerHitbox`
- `EnemyHitbox`
- `Interactable`
- `Projectile`
- `Ground`
- `CameraObstacle`

### Tag

- `Player`
- `Enemy`
- `Checkpoint`
- `Interactable`
- `Pickup`
- `Boss`

## 6. 物理与碰撞规则

第一版建议建立清晰的碰撞矩阵：

- `PlayerHitbox` 只打 `Enemy`
- `EnemyHitbox` 只打 `Player`
- `Projectile` 根据归属只命中合法对象
- 交互检测单独走 `Interactable` 层

不要让受击判定依赖乱用 `CompareTag` 和默认层混搭。

## 7. 脚本模块与 asmdef 建议

当前已创建：

- `CampusRPG.Runtime`
- `CampusRPG.Editor`
- `CampusRPG.Tests.EditMode`
- `CampusRPG.Tests.PlayMode`

模块边界建议：

- `Core`：全局工具、事件、时间、服务定位
- `Character`：玩家运动与状态
- `Combat`：伤害、命中、攻击、量表
- `Skills`：技能定义与施法
- `AI`：敌人与 Boss
- `Save`：章节进度与检查点恢复
- `UI`：HUD、BossBar、提示

## 8. Prefab 与资源命名建议

| 类型 | 示例 |
|---|---|
| 玩家预制体 | `PF_Player` |
| 敌人预制体 | `PF_Enemy_Melee_A` / `PF_Enemy_Mobile_A` / `PF_Enemy_Ranged_A` |
| Boss 预制体 | `PF_Boss_Gatekeeper` |
| 投射物预制体 | `PF_Projectile_SpellBolt` |
| 场景交互物 | `PF_Checkpoint_Standard` |
| 技能配置 | `SO_Skill_SpellBolt` |
| 敌人配置 | `SO_Enemy_Melee` / `SO_Enemy_Mobile` / `SO_Enemy_Ranged` |

## 9. Save 基线

建议第一版保存到：

- `Application.persistentDataPath/Save/slot_auto_chapter01.json`

必须保存：

- 章节 ID
- 检查点 ID
- 关键物品
- 固定成长节点
- 已清理遭遇战

不要在第一版就做多槽、缩略图、复杂元数据。

## 10. 调试与开发工具

必须准备一个开发调试入口，建议形式：

- `DebugPanel` UI
- 或 `F1` 打开的开发菜单

建议功能：

- 回满 HP / MP
- 加满 CounterGauge / AgilityGauge
- 重置技能冷却
- 传送至 `CP01/CP02/CP03`
- 重置当前区域遭遇战
- 强制进入 Boss 房

## 11. 工程初始化完成判定

当以下条件满足时，可视为工程基线已搭好：

1. 正式 Scene 已建立并加入 Build Settings。
2. 正式输入资产已按项目动作重建。
3. `Assets/_Game/` 目录成为唯一正式内容根目录。
4. Layer、Tag、碰撞矩阵已整理。
5. 基础 asmdef 已创建。
6. 调试入口可用。
7. 团队或代理均可通过文档快速理解目录与流程。
